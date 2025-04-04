import os
import cv2
import numpy as np
from PIL import Image
from utils import *
from detections import *

pjoin = os.path.join

asset_dir = os.path.abspath(os.path.join(__file__, '..', 'assets'))

def read_labels(fname):
    with open(fname, 'r') as f:
        return [l.strip() for l in f.read().splitlines()]


VOC_LABELS = read_labels(pjoin(asset_dir, 'VOC_labels.txt'))
COCO_LABELS = read_labels(pjoin(asset_dir, 'COCO_labels.txt'))

providers = [
    'CUDAExecutionProvider',
    'CPUExecutionProvider'
]

class Model:
    def __call__(self, *inputs):
        return self.model(inputs)


class Onnx(Model):
    path = None
    download_url = None

    def __init__(self, path=None):
        # import onnx
        import onnxruntime
        path = pjoin(asset_dir, path or self.path or self.download_url.split('/')[-1])
        if not os.path.isfile(path):
            if self.download_url:
                download_file(self.download_url, path)
        # self.model = onnx.load(path)
        self.sess = sess = onnxruntime.InferenceSession(path, providers = providers)
        self.input_names = [i.name for i in sess.get_inputs()]
        # self.label_name = sess.get_outputs()[0].name
        print('Model inputs:', self.input_names)

    def predict(self, *inputs):
        out = self.sess.run(None, {k: np.asarray(x) for k, x in zip(self.input_names, inputs)})
        return out


class YoloBase(Onnx):
    anchors = []
    threshold = 0.5
    input_size = (416, 416)
    nms_threshold = 0.3


class Yolov2(YoloBase):
    '''A CNN model for real-time object detection system that can detect over 9000 object categories. It uses a single network evaluation, 
    enabling it to be more than 1000x faster than R-CNN and 100x faster than Faster R-CNN. This model is trained with COCO dataset and 
    contains 80 classes.
    '''
    download_url = 'https://github.com/onnx/models/raw/main/vision/object_detection_segmentation/yolov2-coco/model/yolov2-coco-9.onnx'
    labels = COCO_LABELS
    anchors = np.asarray([(1.08, 1.19), (3.42, 4.41), (6.63, 11.38), (9.42, 5.11), (16.62, 10.52)])
    norm = True
    def __call__(self, img):
        img = letterbox_image(img[:,:,:3], self.input_size)
        img = np.transpose(img, [2, 0, 1])
        if self.norm:
            img = img / 255.
        out, = self.predict(img[None].astype('float32'))
        boxes = list(self.postprocess(out[0], self.labels, self.anchors, self.threshold))
        boxes = nms(boxes, self.nms_threshold)
        return [Detection(Box=b) for b in boxes]

    def postprocess(self, Y, labels, anchors, threshold=0.3):
        channel_stride = (len(labels) + 5)
        nch, nx, ny = Y.shape
        assert nch == len(anchors) * channel_stride, (
            "Incorrect output size. Check label and anchor dims: {} * ({} + 5) != {}".format(len(anchors), len(labels), nch))
        for cx in range(nx):
            for cy in range(ny):
                yij = Y[:, cy, cx]
                for b in range(len(anchors)):
                    tx, ty, tw, th, tc, *cls_scores = yij[b * channel_stride:(b + 1) * channel_stride].tolist()
                    confidence = sigmoid(tc)
                    if confidence < threshold:
                        continue
                    i_max = np.argmax(cls_scores)
                    top_score = cls_scores[i_max]
                    if top_score < threshold:
                        continue

                    x = (cx + sigmoid(tx)) / nx
                    y = (cy + sigmoid(ty)) / ny
                    w = np.exp(tw) * anchors[b,0] / nx
                    h = np.exp(th) * anchors[b,1] / ny

                    yield BoundingBox(x - w/2, y - h/2, w, h, labels[i_max], top_score, confidence)


class TinyYolov2(Yolov2):
    '''A real-time CNN for object detection that detects 20 different classes (VOC). A smaller version of the more complex full YOLOv2 network.
    '''
    download_url = 'https://github.com/onnx/models/raw/main/vision/object_detection_segmentation/tiny-yolov2/model/tinyyolov2-8.onnx'
    labels = VOC_LABELS
    norm = False
    


class Yolov3(YoloBase):
    '''A deep CNN model for real-time object detection that detects 80 different classes. A little bigger than 
    YOLOv2 but still very fast. As accurate as SSD but 3 times faster.
    '''
    download_url = 'https://github.com/onnx/models/raw/main/vision/object_detection_segmentation/yolov3/model/yolov3-10.onnx'
    labels = COCO_LABELS

    def __call__(self, img):
        img = letterbox_image(img, self.input_size)
        # img = cv2.resize(img, self.input_size)
        img = np.transpose(img / 255., [2, 0, 1])[None,:3].astype('float32')
        boxes, scores, indices = self.predict(img, np.asarray(img.shape[-2:][::-1], dtype='float32').reshape(1, 2))
        boxes = list(self.postprocess(boxes, scores, indices, img.shape[-2:]))
        boxes = nms(boxes, self.nms_threshold)
        return [Detection(Box=b) for b in boxes]

    def postprocess(self, boxes, scores, indices, shape):
        ih, iw = shape
        for ibatch, icls, ibox in indices:
            y, x, y2, x2 = boxes[ibatch, ibox]
            score = scores[ibatch, icls, ibox]
            if score < self.threshold:
                continue
            yield BoundingBox(
                x/iw, y/ih, (x2-x)/iw, (y2-y)/ih, self.labels[icls], 1, score)


class TinyYolov3(Yolov3):
    '''A smaller version of YOLOv3 model.'''
    download_url = 'https://github.com/onnx/models/raw/main/vision/object_detection_segmentation/tiny-yolov3/model/tiny-yolov3-11.onnx'
    labels = COCO_LABELS
    def postprocess(self, boxes, scores, indices, shape):
        return super().postprocess(boxes, scores, indices[0], shape)


class Yolov4(YoloBase):
    '''Optimizes the speed and accuracy of object detection. Two times faster than EfficientDet. It improves 
    YOLOv3's AP and FPS by 10% and 12%, respectively, with mAP50 of 52.32 on the COCO 2017 dataset and FPS 
    of 41.7 on a Tesla V100.
    '''
    download_url = 'https://github.com/onnx/models/raw/main/vision/object_detection_segmentation/yolov4/model/yolov4.onnx'
    labels = COCO_LABELS
    anchors = np.array([
        [[12,16], [19,36], [40,28]], 
        [[36,75], [76,55], [72,146]], 
        [[142,110], [192,243], [459,401]],
    ])
    xyscale = [1.2, 1.1, 1.05]
    threshold = 0.6
    # anchors = '12,16, 19,36, 40,28, 36,75, 76,55, 72,146, 142,110, 192,243, 459,401'
    # def __call__(self, x):
    #     x = letterbox_image(x, self.input_size) / 255.
    #     return self.model(x)

    def __call__(self, img):
        img = letterbox_image(img[:,:,:3], self.input_size) / 255.
        # img = img[:,:,:3] / 255.
        out = self.predict(img[None].astype('float32'))
        boxes = [
            b for anchor, x, xyscale in zip(self.anchors, out, self.xyscale)
            for b in self.postprocess(x[0], self.labels, anchor, xyscale, self.threshold)
        ]
        boxes = nms(boxes, self.nms_threshold)
        return [Detection(Box=b) for b in boxes]

    # def postprocess(self, pred_bbox, anchors, xyscale=[1,1,1], threshold=0.5):
    #     '''define anchor boxes'''
    #     for i, pred in enumerate(pred_bbox):
    #         nx, ny, nanch = pred.shape[1:3]
    #         xy_grid = np.stack(np.meshgrid(np.arange(nx), np.arange(ny)), axis=-1)
    #         xy_grid = np.concatenate([xy_grid[...,None,:]]*nanch, axis=-2).astype(np.float)[None]
    #         # xy and wh
    #         pred[..., 0:2] = (xy_grid + (sigmoid(pred[..., 0:2]) - 1/2) * xyscale[i] + 1/2) / np.array([nx, ny])
    #         pred[..., 2:4] = (np.exp(pred[..., 2:4]) * anchors[i]) / np.array([nx, ny])

    #     for x in pred_bbox:
    #         for yb in np.reshape(x, (-1, x.shape[-1])):
    #             x, y, w, h, tc, *cls_scores = yb
    #             i_max = np.argmax(cls_scores)
    #             top_score = cls_scores[i_max]
    #             if top_score < threshold:
    #                 continue
    #             yield BoundingBox(x - w/2, y - h/2, w, h, labels[i_max], top_score, confidence)

    #     return np.concatenate([np.reshape(x, (-1, x.shape[-1])) for x in pred_bbox], axis=0)

    def postprocess(self, Y, labels, anchors, xyscale, threshold):
        nx, ny, nanch, nfeat = Y.shape
        assert nanch == len(anchors), f"Wrong number of anchors {len(anchors)} != {nanch}"
        assert nfeat == len(labels) + 5, f"Wrong number of labels {len(labels)} + 5 != {nfeat}"
        for cx in range(nx):
            for cy in range(ny):
                for b in range(len(anchors)):
                    tx, ty, tw, th, tc, *cls_scores = Y[cy, cx, b]
                    confidence = tc#sigmoid(tc)
                    if confidence < threshold:
                        continue
                    i_max = np.argmax(cls_scores)
                    top_score = cls_scores[i_max]
                    if top_score < threshold:
                        continue
                    # print(confidence, tc, labels[i_max])
                    x = (cx + (sigmoid(tx) - 1/2) * xyscale + 1/2) / nx
                    y = (cy + (sigmoid(ty) - 1/2) * xyscale + 1/2) / ny
                    w = np.exp(tw) * anchors[b,0] / nx
                    h = np.exp(th) * anchors[b,1] / ny
                    # print(x, y, w, h)
                    # print(x, y, w, h)
                    yield BoundingBox(x - w/2, y - h/2, w, h, labels[i_max], top_score, confidence)

    # def postprocess_bbbox(self, pred_bbox, ANCHORS, STRIDES, XYSCALE=[1,1,1]):
    #     # (1, 52, 52, 3, 85)
    #     for i, pred in enumerate(pred_bbox):
    #         conv_shape = pred.shape
    #         output_size = conv_shape[1]
    #         conv_raw_dxdy = pred[:, :, :, :, 0:2]
    #         conv_raw_dwdh = pred[:, :, :, :, 2:4]
    #         xy_grid = np.meshgrid(np.arange(output_size), np.arange(output_size))
    #         xy_grid = np.expand_dims(np.stack(xy_grid, axis=-1), axis=2)
    #         xy_grid = np.tile(xy_grid[None], [1, 1, 1, 3, 1]).astype(np.float)
    #         pred_xy = ((special.expit(conv_raw_dxdy) * XYSCALE[i]) - 0.5 * (XYSCALE[i] - 1) + xy_grid) * STRIDES[i]
    #         pred[:, :, :, :, :4] = np.concatenate([
    #             pred_xy, 
    #             np.exp(conv_raw_dwdh) * self.anchors[i]
    #         ], axis=-1)

    #     return np.concatenate([
    #         np.reshape(x, (-1, np.shape(x)[-1])) for x in pred_bbox], axis=0)


# def postprocess_bbox(boxes, anchors):  # 52, 52, 3, 85
#     # separate outputs
#     ny, nx = boxes.shape[:2]
#     xy = boxes[...,:2]
#     wh = boxes[...,2:4]
#     conf = boxes[...,4]
#     cls_scores = boxes[...,5:]

#     xy_grid = np.stack(np.meshgrid(np.arange(ny), np.arange(nx)), -1)
#     xy = (sigmoid(xy) + xy_grid) / np.array([ny, nx])
#     wh = np.exp(wh) * anchors

    


#     channel_stride = (len(labels) + 5)
#     nch, nx, ny = Y.shape
#     assert nch == len(anchors) * channel_stride, (
#         "Incorrect output size. Check label and anchor dims: {} * ({} + 5) != {}".format(len(anchors), len(labels), nch))
#     for cx in range(nx):
#         for cy in range(ny):
#             yij = Y[:, cy, cx]
#             for b in range(len(anchors)):
#                 tx, ty, tw, th, tc, *cls_scores = yij[b * channel_stride:(b + 1) * channel_stride].tolist()
#                 confidence = sigmoid(tc)
#                 if confidence < threshold:
#                     continue
#                 i_max = np.argmax(cls_scores)
#                 top_score = cls_scores[i_max]
#                 if top_score < threshold:
#                     continue

#                 x = (cx + sigmoid(tx)) / nx
#                 y = (cy + sigmoid(ty)) / ny
#                 w = np.exp(tw) * anchors[b,0] / nx
#                 h = np.exp(th) * anchors[b,1] / ny

#                 yield BoundingBox(x - w/2, y - h/2, w, h, labels[i_max], top_score, confidence)


def letterbox_image(im, size, color=(0, 0, 0)):
    # TODO: Need to apply inverse letterbox transform to boxes, until then, we'll just stretch
    return cv2.resize(im, size)
    th, tw = size  # target
    h, w = im.shape[:2]  # current
    if (th, tw) == (h, w): 
        return im
    ratio = min(th / h, tw / w)
    nh, nw = int(h * ratio), int(w * ratio)  # new
    dh, dw = th - nh, tw - nw  # diff
    return cv2.copyMakeBorder(
        cv2.resize(im, (nw, nh)), 
        dh//2, -(dh//-2), dw//2, -(dw//-2), 
        cv2.BORDER_CONSTANT, value=color)


def sigmoid(x):
  return 1 / (1 + np.exp(-x))





models = {
    'tinyyolov2': TinyYolov2,
    'yolov2': Yolov2,
    'yolov3': Yolov3,
    'tinyyolov3': TinyYolov3,
    'yolov4': Yolov4,
}



def get_model(name):
    model = models[name.lower()]()
    return model

def show(*names):
    import inspect
    for name, m in [(n, models[n]) for n in names] or models.items():
        print('-'*20)
        print(f'{name} : {m.__name__}')
        print(inspect.cleandoc(m.__doc__), '\n')
        if getattr(m, 'download_url', None):
            print(f'\tDownload url:', m.download_url)
        if getattr(m, 'labels', None):
            print(f'\tLabels: ({len(m.labels)})', m.labels)
        print()
        print()


def download_file(url, local_filename=None):
    import requests
    local_filename = local_filename or url.split('/')[-1]
    print(f"Downloading {local_filename} to {url} ...")
    with requests.get(url, stream=True) as r:
        r.raise_for_status()
        with open(local_filename, 'wb') as f:
            for chunk in r.iter_content(chunk_size=8192): 
                # If you have chunk encoded response uncomment if
                # and set chunk_size parameter to None.
                #if chunk: 
                f.write(chunk)
    print('download finished.')
    return local_filename