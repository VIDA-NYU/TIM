import os
import sys
import pathlib

DIR = pathlib.Path(__file__).parent

import json

import torch
import torchvision.transforms as T
from PIL import Image
from pytorchvideo.data.encoded_video import EncodedVideo
from torchvision.transforms._transforms_video import NormalizeVideo
from pytorchvideo.transforms import (ApplyTransformToKey, ShortSideScale, UniformTemporalSubsample)

omnivore_git_path = DIR / "omnivore.nogit"
sys.path.append(str(omnivore_git_path))
try:
    from transforms import SpatialCrop, TemporalCrop, DepthNorm
except:
    os.system(f'git clone https://github.com/facebookresearch/omnivore.git "{omnivore_git_path}"')
    os.system(f'{sys.executable} -m pip install einops timm')
    from transforms import SpatialCrop, TemporalCrop, DepthNorm

import matplotlib.pyplot as plt
# import matplotlib.image as mpimg
# from ipywidgets import Video


# num_frames = 160
# sampling_rate = 2
# frames_per_second = 30

# clip_duration = (num_frames * sampling_rate) / frames_per_second




has_gpu = torch.cuda.is_available()

class Omnivore:
    def __init__(self, model_name="omnivore_swinB", num_frames=160):
        self.device = device = 'cuda' if has_gpu else 'cpu'
        # Pick a pretrained model 
        model = torch.hub.load("facebookresearch/omnivore:main", model=model_name)
        # Set to eval mode and move to desired device
        model = model.to(device)
        model = model.eval()
        self.model = model

        # Create an id to label name mapping

        with open(DIR / "imagenet_class_index.json", "r") as f:
            imagenet_classnames = json.load(f)
        self.imagenet2label = {str(k): v[1] for k, v in imagenet_classnames.items()}
        # print('imagenet2label', set(type(k).__name__ for k in self.imagenet2label))
        df = set(self.imagenet2label) - set(map(str, range(len(self.imagenet2label))))
        assert not df, df

        with open(DIR / "kinetics_classnames.json", "r") as f:
            kinetics_classnames = json.load(f)
        self.kinetics2label = {str(v): str(k).replace('"', "") for k, v in kinetics_classnames.items()}
        # print('kinetics2label', set(type(k).__name__ for k in self.kinetics2label))
        df = set(self.kinetics2label) - set(map(str, range(len(self.kinetics2label))))
        assert not df, df

        with open(DIR / "sunrgbd_classnames.json", "r") as f:
            self.sunrgbd2label = json.load(f)
        # print('sunrgbd2label', set(type(k).__name__ for k in self.sunrgbd2label))
        df = set(self.sunrgbd2label) - set(map(str, range(len(self.sunrgbd2label))))
        assert not df, df

        # transformers for different input types

        self.image_transform = T.Compose([
            T.Resize(224),
            T.CenterCrop(224),
            T.ToTensor(),
            T.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
        ])

        self.video_transform = T.Compose([
            UniformTemporalSubsample(num_frames), 
            T.Lambda(lambda x: x / 255.0),  
            ShortSideScale(size=224),
            NormalizeVideo(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
            TemporalCrop(frames_per_clip=32, stride=40),
            SpatialCrop(crop_size=224, num_crops=3),
        ])
        # ApplyTransformToKey(key="video", transform=)

        self.rgbd_transform = T.Compose([
            DepthNorm(max_depth=75.0, clamp_max_before_scale=True),
            T.Resize(224),
            T.CenterCrop(224),
            T.Normalize(
                mean=[0.485, 0.456, 0.406, 0.0418], 
                std=[0.229, 0.224, 0.225, 0.0295]
            ),
        ])

    def predict_image(self, image):
        image = self.image_transform(image)[None, :, None, ...]
        return self._predict_top_k(image, 'image', self.imagenet2label)

    def predict_video(self, video):
        # Apply a transform to normalize the video input
        video_data = self.video_transform(video)[0][None, ...]
        return self._predict_top_k(video_data, 'video', self.kinetics2label)

    def predict_rgbd(self, image, depth):
        # Convert to tensor and transform
        image = T.ToTensor()(image)
        rgbd = torch.cat([image, depth], dim=0)
        rgbd = self.rgbd_transform(rgbd)[None, :, None, ...]        
        return self._predict_top_k(rgbd, 'rgbd', self.sunrgbd2label)

    def _predict_top_k(self, input, input_type, cls_map, k=5):
        # The model expects inputs of shape: B x C x T x H x W
        print(f'{input_type} shape:', input.shape)
        with torch.no_grad():
            prediction = self.model(input.to(self.device), input_type=input_type)
            pred_classes = prediction.topk(k=k).indices

        pred_class_names = [cls_map[str(i.item())] for i in pred_classes[0]]
        # print("Top 5 predicted labels: %s" % ", ".join(pred_class_names))
        return pred_class_names


import time
import contextlib

@contextlib.contextmanager
def timed(msg=''):
    try:
        t0 = time.time()
        print(f'-- {msg} ...')
        yield
    finally:
        print(f'-- {msg} - took {time.time() - t0:0.5g}s')


def original_demo():
    model = Omnivore()
    image_demo(model, DIR / "library.jpg")
    video_demo(model, DIR / "dance.mp4")
    depth_demo(model, DIR / "store.png", DIR / "store_disparity.pt")


def image_demo(model, path):
    image = Image.open(path).convert("RGB")
    with timed(f'predict {path}'):
        labels = model.predict_image(image)

    print(path, "Top 5 predicted labels: %s" % ", ".join(labels))

    plt.figure(figsize=(10, 10))
    plt.imshow(image)
    plt.title(', '.join(labels))
    plt.show()


def video_demo(model, path, offset=0, duration=0.1):
    # Select the duration of the clip to load by specifying the start and end duration
    # The start_sec should correspond to where the action occurs in the video

    # Initialize an EncodedVideo helper class
    video = EncodedVideo.from_path(path)
    video_data = video.get_clip(start_sec=offset, end_sec=offset+duration)['video']

    with timed(f'predict {path}'):
        labels = model.predict_video(video_data)

    print(path, "Top 5 predicted labels: %s" % ", ".join(labels))

    plt.figure(figsize=(10, 10))
    plt.imshow(video_data[0].permute(1, 2, 0) / 255)
    plt.title(', '.join(labels))
    plt.show()


def depth_demo(model, path, depth):
    image = Image.open(path).convert("RGB")
    depth = torch.load(depth)[None, ...]

    with timed(f'predict {path}'):
        labels = model.predict_rgbd(image, depth)

    print(path, "Top 5 predicted labels: %s" % ", ".join(labels))

    plt.figure(figsize=(20, 10))
    plt.subplot(1, 2, 1)
    plt.title("RGB", fontsize=20)
    plt.imshow(image)
    plt.subplot(1, 2, 2)
    plt.imshow(depth.numpy().squeeze())
    plt.title("Depth", fontsize=20)
    plt.suptitle(', '.join(labels))
    plt.tight_layout()
    plt.show()


def demo(path=None, type=None, *a, **kw):
    if not path:
        return original_demo()
    model = Omnivore()
    if type == 'image':
        return image_demo(model, path, *a, **kw)
    if type == 'video':
        return video_demo(model, path, *a, **kw)
    if type == 'depth':
        return depth_demo(model, path, *a, **kw)
    raise ValueError(f'Invalid input type {type} - one of: image, video, depth')

if __name__ == '__main__':
    import fire
    fire.Fire(demo)