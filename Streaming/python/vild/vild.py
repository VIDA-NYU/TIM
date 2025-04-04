import os
import time
import numpy as np
from tqdm import tqdm
from PIL import Image

import cv2
import torch
import clip
import tensorflow.compat.v1 as tf

from scipy.special import softmax
import vis


single_template = ['a photo of {article} {}.']

multiple_templates = [
    'There is {article} {} in the scene.',
    'There is the {} in the scene.',
    'a photo of {article} {} in the scene.',
    'a photo of the {} in the scene.',
    'a photo of one {} in the scene.',


    'itap of {article} {}.',
    'itap of my {}.',  # itap: I took a picture of
    'itap of the {}.',
    'a photo of {article} {}.',
    'a photo of my {}.',
    'a photo of the {}.',
    'a photo of one {}.',
    'a photo of many {}.',

    'a good photo of {article} {}.',
    'a good photo of the {}.',
    'a bad photo of {article} {}.',
    'a bad photo of the {}.',
    'a photo of a nice {}.',
    'a photo of the nice {}.',
    'a photo of a cool {}.',
    'a photo of the cool {}.',
    'a photo of a weird {}.',
    'a photo of the weird {}.',

    'a photo of a small {}.',
    'a photo of the small {}.',
    'a photo of a large {}.',
    'a photo of the large {}.',

    'a photo of a clean {}.',
    'a photo of the clean {}.',
    'a photo of a dirty {}.',
    'a photo of the dirty {}.',

    'a bright photo of {article} {}.',
    'a bright photo of the {}.',
    'a dark photo of {article} {}.',
    'a dark photo of the {}.',

    'a photo of a hard to see {}.',
    'a photo of the hard to see {}.',
    'a low resolution photo of {article} {}.',
    'a low resolution photo of the {}.',
    'a cropped photo of {article} {}.',
    'a cropped photo of the {}.',
    'a close-up photo of {article} {}.',
    'a close-up photo of the {}.',
    'a jpeg corrupted photo of {article} {}.',
    'a jpeg corrupted photo of the {}.',
    'a blurry photo of {article} {}.',
    'a blurry photo of the {}.',
    'a pixelated photo of {article} {}.',
    'a pixelated photo of the {}.',

    'a black and white photo of the {}.',
    'a black and white photo of {article} {}.',

    'a plastic {}.',
    'the plastic {}.',

    'a toy {}.',
    'the toy {}.',
    'a plushie {}.',
    'the plushie {}.',
    'a cartoon {}.',
    'the cartoon {}.',

    'an embroidered {}.',
    'the embroidered {}.',

    'a painting of the {}.',
    'a painting of a {}.',
]


VILD_NOTEBOOK_URL = 'https://colab.research.google.com/github/tensorflow/tpu/blob/master/models/official/detection/projects/vild/ViLD_demo.ipynb'

class Vild:
    def __init__(self, saved_model_dir='./image_path_v2'):
        if not os.path.isfile(saved_model_dir):
            raise OSError(f"You're missing the weights files. Download ``image_path_v2/*`` from: \n    {VILD_NOTEBOOK_URL}")
        self.session = session = tf.Session(graph=tf.Graph())
        _ = tf.saved_model.loader.load(session, ['serve'], saved_model_dir)
        clip.available_models()
        self.model, preprocess = clip.load("ViT-B/32")


    def main(self, image_path, category_names, min_rpn_score_thresh=0.9, overall_fig_size=(6, 12), max_boxes_to_draw=3):
        category_names = as_category_names(category_names)
        text_features = self.embed_text(category_names)

        # detection_roi_scores, detection_boxes, detection_masks, detection_visual_feat, rescaled_detection_boxes, valid_indices
        dets = self.predict(image_path, min_rpn_score_thresh=min_rpn_score_thresh)
        
        print(len(dets.shapes), dets.shapes)
        dets.text_scores, dets.text_indices, indices_fg = self.text_scores(text_features, dets.features)
        print(dets.text_scores, dets.text_indices, indices_fg)
        print(dets.text_indices.shape, indices_fg.shape, dets.text_scores.shape)
        # print(len(dets.shapes), dets.shapes)

        # plotting
        vis.display_image_and_cropped_annotations(
            image=np.asarray(Image.open(open(image_path, 'rb')).convert("RGB")), 
            indices=dets.text_indices, 
            rescaled_detection_boxes=dets.scaled_boxes, 
            detection_masks=dets.masks, 
            detection_roi_scores=dets.roi_scores, 
            scores_all=dets.text_scores, 
            category_names=category_names,
            indices_fg=indices_fg, 
            valid_indices=dets.valid_indices, 
            overall_fig_size=overall_fig_size, 
            min_rpn_score_thresh=min_rpn_score_thresh, 
            max_boxes_to_draw=max_boxes_to_draw)

    def predict(self, image_path, nms_threshold=0.6, min_rpn_score_thresh=0.9, min_box_area=220):
        t = time.time()
        # Obtain results and read image
        (roi_boxes, roi_scores, detection_boxes, scores_unused, box_outputs, 
         detection_masks, visual_features, image_info) = self.session.run(
            ['RoiBoxes:0', 'RoiScores:0', '2ndStageBoxes:0', '2ndStageScoresUnused:0', 'BoxOutputs:0', 
             'MaskOutputs:0', 'VisualFeatOutputs:0', 'ImageInfo:0'],
            feed_dict={'Placeholder:0': [image_path,]})
        print(f'Took {time.time() - t:.3g} secs.')

        # print('roi_boxes', roi_boxes.shape)                  (1, 1000, 4)
        # print('roi_scores', roi_scores.shape)                (1, 1000)
        # print('detection_boxes', detection_boxes.shape)      (1, 1000, 1, 4)
        # print('scores_unused', scores_unused.shape)          (1, 1000, 1203)
        # print('box_outputs', box_outputs.shape)              (1, 1000, 4)
        # print('detection_masks', detection_masks.shape)      (1, 1000, 28, 28)
        # print('visual_features', visual_features.shape)      (1, 1000, 512)
        # print('image_info', image_info.shape)                (1, 4, 2)

        dets = Detections(detection_boxes[:,:,0], roi_boxes, roi_scores, detection_masks, visual_features)[0]
        sx, sy = image_info[0, 2, :]
        dets.scaled_boxes = dets.boxes / np.array([[sx, sy, sx, sy]]) # rescale
        # Apply non-maximum suppression to detected boxes with nms threshold.
        valid_indices = nms(
            dets.boxes, dets.roi_scores, nms_threshold,
            min_box_area * sx*sy, 
            min_rpn_score_thresh)
        print('number of valid indices', len(valid_indices))
        valid_indices = valid_indices
        dets = dets[valid_indices]
        dets.valid_indices = valid_indices
        return dets


    def text_scores(self, text_features, detection_visual_feat, temperature=100., use_softmax=False):
        '''Get correlation between text embeddings and image features.'''
        # [] X [].T
        raw_scores = detection_visual_feat.dot(text_features.T)
        text_scores = softmax(temperature * raw_scores, axis=-1) if use_softmax else raw_scores
        indices = np.argsort(-np.max(text_scores, axis=1))  # Results are ranked by scores
        indices_fg = np.array([i for i in indices if np.argmax(text_scores[i]) != 0])
        return text_scores, indices, indices_fg

    def embed_text(self, categories, prompt_engineering=True, this_is=True):
        templates = multiple_templates if prompt_engineering else single_template
        run_on_gpu = torch.cuda.is_available()
        with torch.no_grad():
            all_text_embeddings = []
            print('Building text embeddings...')
            for category in tqdm(categories):
                cat = category if isinstance(category, str) else category['name']
                texts = [
                    tpl.format(processed_name(cat, rm_dot=True), article=article(cat))
                    for tpl in templates]
                if this_is:
                    texts = [
                        f'This is {t}' if t.startswith('a') or t.startswith('the') else t 
                        for t in texts]
                texts = clip.tokenize(texts) #tokenize
                if run_on_gpu:
                    texts = texts.cuda()
                text_embeddings = self.model.encode_text(texts) #embed with text encoder
                text_embeddings /= text_embeddings.norm(dim=-1, keepdim=True)
                text_embedding = text_embeddings.mean(dim=0)
                text_embedding /= text_embedding.norm()
                all_text_embeddings.append(text_embedding)
            all_text_embeddings = torch.stack(all_text_embeddings, dim=1)
            if run_on_gpu:
                all_text_embeddings = all_text_embeddings.cuda()
        return all_text_embeddings.cpu().numpy().T


import dataclasses
@dataclasses.dataclass
class Detections:
    # n_boxes=1000, n_valid=
    boxes: np.ndarray                 # (1, n_boxes, 4)
    roi_boxes: np.ndarray             # (1, n_boxes, 4)
    roi_scores: np.ndarray            # (1, n_boxes)
    masks: np.ndarray                 # (1, n_boxes, 28, 28)
    features: np.ndarray              # (1, n_boxes, 512)
    scaled_boxes: np.ndarray = None   # (1, n_boxes, 4) / image_scale
    # after we slice with valid_indices
    valid_indices: np.ndarray = None  # (n_valid,) used to slice 1000 so will match up
    text_scores: np.ndarray = None    # (n_valid, n_classes)
    text_indices: np.ndarray = None   # (n_valid,)

    @property
    def shapes(self):
        return [x.shape if x is not None else () for x in dataclasses.astuple(self)]

    @property
    def xs(self):
        return dataclasses.astuple(self) #tuple(x for x in  if x is not None)

    def __getitem__(self, index):
        '''Slice all arrays'''
        xs = dataclasses.astuple(self)
        return self.__class__(*(x[index] if x is not None and len(x) == len(xs[0]) else x for x in xs))

    def __iter__(self):
        '''Like zipping arrays'''
        for i in range(len(self.xs[0])):
            yield self[i]





def article(name):
    return 'an' if name[0] in 'aeiou' else 'a'

def processed_name(name, rm_dot=False):  # _ for lvis      / for obj365
    res = name.replace('_', ' ').replace('/', ' or ').lower()
    if rm_dot:
        res = res.rstrip('.')
    return res


def as_category_names(category_names):
    return ['background'] + [x.strip() for x in (category_names.split(';') if isinstance(category_names, str) else category_names)]


#@title NMS
def nms(boxes, scores, thresh, min_box_area, min_rpn_score_thresh, max_dets=1000):
    """Non-maximum suppression.
    Args:
        dets: [N, 4]
        scores: [N,]
        thresh: iou threshold. Float
        max_dets: int.
    """
    y1, x1, y2, x2 = boxes.T
    areas = (x2 - x1) * (y2 - y1)
    order = scores.argsort()[::-1]

    keep = []
    while order.size > 0 and len(keep) < max_dets:
        i, js = order[0], order[1:]
        keep.append(i)
        w = np.maximum(0.0, np.minimum(x2[i], x2[js]) - np.maximum(x1[i], x1[js]))
        h = np.maximum(0.0, np.minimum(y2[i], y2[js]) - np.maximum(y1[i], y1[js]))
        intersect = w * h
        overlap = intersect / (areas[i] + areas[js] - intersect + 1e-12)
        order = order[np.where(overlap <= thresh)[0] + 1]

    nmsed = np.isin(np.arange(len(scores), dtype=np.int), keep)
    nonzero_boxes = ~np.all(boxes == 0., axis=-1)
    high_scores = scores >= min_rpn_score_thresh
    big_enough = areas > min_box_area
    print('areas', np.min(areas), np.mean(areas), np.max(areas), '>', min_box_area, np.sum(big_enough))
    return np.where(nmsed & nonzero_boxes & high_scores & big_enough)[0]



def main(image, *categories, **kw):
    image = image or os.path.join(__file__, '../../assets/ingredients.jpg')
    categories = categories or ['tomato', 'garlic', 'chili pepper', 'giraffe', 'cucumber']
    vild = Vild()
    vild.main(image, categories, **kw)

if __name__ == '__main__':
    import fire
    fire.Fire(main)