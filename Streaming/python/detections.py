import dataclasses
from typing import List
import numpy as np
import cv2
from utils import recvall
import matplotlib.pyplot as plt
import msgpack
# try:
#     import msgpack
#     import msgpack_numpy
#     msgpack_numpy.patch()
#     HAS_MSGPACK = True
# except ImportError:
#     import warnings
#     warnings.warn("You don't have msgpack installed. You won't be able to serialize detection classes.")
#     HAS_MSGPACK = False



@dataclasses.dataclass
class BoundingBox:
    # top left and width height
    X: float
    Y: float
    Width: float
    Height: float
    Label: str = None
    LabelScore: float = 1
    Confidence: float = 1

    def draw_cv(self, im, color=(0,255,0)):
        iy, ix = im.shape[:2]
        x, y, h, w = self.X, self.Y, self.Height, self.Width
        x, y, x2, y2 = (
            int(ix * max(x, 0)), int(iy * max(y, 0)), 
            int(ix * min(x+w, 1)), int(iy * min(y+h, 1)))
        # print(self.label, self.label_score, self.confidence)
        cv2.rectangle(im, (x, y), (x2, y2), color, 2)
        if self.Label:
            cv2.rectangle(im, (x + 4, y - 6), (x + 4 + 2 + 8*len(self.Label), y + 6), color, -1)
            cv2.putText(im, self.Label, (x + 10, y + 2), 0, 0.3, (0, 0, 0))
        return im

# @dataclasses.dataclass
# class Contour:
#     pts: np.ndarray
#     label: str = None
#     def draw_cv(self, im, color=(0,255,0)): 
#         return im




# class MASK_OPTIONS:
#     '''Not sure how to manage this? we don't ewant to send this info
#     mask = alpha if constant_alpha else (
#         clip( scale * (activations + offset), floor, alpha )
#     )
    
#     '''
#     alpha=0.7  # the alpha of the mask. if not constant_alpha, this is the maximum alpha
#     alpha_scale=5  # this makes the activations approach maximum alpha faster. Use to scale activations.
#     alpha_offset=0  # this lets you shift the activations to have a higher or lower alpha
#     alpha_floor=0  # a minimum alpha
#     constant_alpha=False  # use alpha as a constant transparency, or as the maximum transparency?
#     cmap = plt.get_cmap('magma')

# @dataclasses.dataclass
# class Mask:
#     mask: np.ndarray
#     label: str = None
#     def draw_cv(self, im): 
#         im = cv2.resize(self.mask, im.shape)
#         cm_im = MASK_OPTIONS.cmap(im)
#         cm_im, mask = cm_im[...,:-1], cm_im[...,-1:]
#         mask = mask * MASK_OPTIONS.alpha if MASK_OPTIONS.constant_alpha else np.clip(
#             (im + MASK_OPTIONS.alpha_offset) * MASK_OPTIONS.alpha_scale, 
#             MASK_OPTIONS.alpha_floor, MASK_OPTIONS.alpha)
#         return im * (1 - mask) + cm_im * mask

@dataclasses.dataclass
class Detection:
    Box: BoundingBox = None
    # contour: Contour = None

    def draw_cv(self, im, *a, **kw):
        if self.Box is not None:
            im = self.Box.draw_cv(im, *a, **kw)
        # if self.contour is not None:
        #     im = self.contour.draw_cv(im, *a, **kw)
        return im

    # @classmethod
    # def from_items(cls, box=None, contour=None):
    #     return cls(
    #         BoundingBox(*box) if box else None,
    #         # Contour(*contour) if contour else None
    #     )

    @classmethod
    def from_items(cls, Box=None, contour=None):
        return cls(
            BoundingBox(**Box) if Box else None,
            # Contour(**contour) if contour else None
        )





@dataclasses.dataclass
class FrameResults:
    Detections: List[Detection]
    Texts: List[dict] 

    def draw_cv(self, im, **kw):
        for d in self.Detections:
            d.draw_cv(im, **kw)
        if self.Texts:
            t = self.Texts[0]
            cv2.putText(
                im, t['Content'], 
                (10,100), cv2.FONT_HERSHEY_SIMPLEX, 3, (0,255,0) if t.get('Detected') else (255,255,255), 5, 2)
        return im

    @classmethod
    def from_items(cls, Detections=None, Texts=None):
        return cls([Detection.from_items(**d) for d in Detections] if Detections else [], Texts)



def nms(boxes, threshold=0.3):
    n_active = len(boxes)
    active = [True]*len(boxes)
    boxes = sorted(boxes, key=lambda b: b.Confidence, reverse=True)
    
    for i, bA in enumerate(boxes):
        if not n_active: break
        if not active[i]: continue
        for j, bB in enumerate(boxes[i+1:], i+1):
            if not active[j]: continue
            if IoU(bA, bB) > threshold:
                active[j] = False
                n_active -= 1
    # print('keep', [b.confidence for i, b in enumerate(boxes) if active[i]], 'remove', [b.confidence for i, b in enumerate(boxes) if not active[i]])
    return [b for i, b in enumerate(boxes) if active[i]]


def IoU(a, b):
    areaA = a.Width * a.Height
    areaB = b.Width * b.Height
    if areaA <= 0 or areaB <= 0: return 0
    intersectionArea = (
        max(min(a.Y + a.Height, b.Y + b.Height) - max(a.Y, b.Y), 0) * 
        max(min(a.X + a.Width, b.X + b.Width) - max(a.X, b.X), 0))
    return intersectionArea / (areaA + areaB - intersectionArea)







import json

class EnhancedJSONEncoder(json.JSONEncoder):
    def default(self, o):
        if dataclasses.is_dataclass(o):
            return dataclasses.asdict(o)
        if isinstance(o, np.ndarray):
            return o.tolist()
        if isinstance(o, np.generic):
            return o.item()
        return super().default(o)


def json_dump(obj):
    return json.dumps(obj, cls=EnhancedJSONEncoder).encode('utf-8')

# def dict2dataclass(cls, **obj):
#     fs = dataclasses
#     for k, v in obj.items():
#         if isinstance(v, (list, tuple)):
#             obj[k] = [dict2dataclass(clsi, x) for x in v]
#     return cls(**obj)


def write_json(writer, obj):
    data = json_dump(obj)
    writer.send(np.uint32(len(data)).tobytes() + data)

def read_json(reader):
    l = int(np.frombuffer(recvall(reader, 4), np.uint32))
    return json.loads(recvall(reader, l).decode('utf-8'))


write_data = write_json
read_data = read_json





# def dc_pack(obj):
#     return msgpack.packb(_dc_pack(obj))

# def _dc_pack(obj):
#     if dataclasses.is_dataclass(obj):
#         return tuple(_dc_pack(x) for x in dataclasses.astuple(obj))
#     if isinstance(obj, (list, tuple)):
#         return [_dc_pack(x) for x in obj]
#     return obj

# def nested_apply(x, func, *a, **kw):
#     if isinstance(x, (list, tuple, set)):
#         return type(x)(nested_apply(xi, func, *a, **kw) for xi in x)
#     if isinstance(x, dict):
#         return {k: nested_apply(xi, func, *a, **kw) for k, xi in x.items()}
#     return x


# def dc_tuple(x):
#     if dataclasses.is_dataclass(x):
#         return tuple(nested_apply(x, dc_tuple) for x in dataclasses.astuple(x))
#     return x

# def dc_dict(x):
#     if dataclasses.is_dataclass(x):
#         return {k: nested_apply(v, dc_tuple) for k, v in dataclasses.asdict(x)}
#     return x


# def dc_tuple(obj):
#     if dataclasses.is_dataclass(obj):
#         return tuple(dc_tuple(x) for x in dataclasses.astuple(obj))
#     if isinstance(obj, (list, tuple)):
#         return [dc_tuple(x) for x in obj]
#     return obj


# def dc_unpack(cls, obj):
#     obj
#     msgpack.unpackb(obj)
#     return msgpack.packb(_dc_pack(obj))

# def _dc_unpack(cls, obj):
#     if dataclasses.is_dataclass(cls):
#         return cls(*(_dc_unpack(f.type, x) for f, x in zip(cls._fields, obj)))
#     return obj


# def custom_encode(x):
#     if isinstance(x, np.ndarray):
#         return x.tolist()
#     if isinstance(x, np.generic):
#         return x.item()
#     return x

# def write_data(writer, obj):
#     data = msgpack.packb(dc_tuple(obj), default=custom_encode)
#     writer.send(np.uint32(len(data)).tobytes() + data)

# def read_data(reader):
#     l = int(np.frombuffer(recvall(reader, 4), np.uint32))
#     return msgpack.unpackb(recvall(reader, l))

# @dataclasses.dataclass
# class SerializeHeader:
#     length: np.uint32

#     @classmethod
#     def pack_data(cls, obj, writer):
#         # writer.send(dc_pack(obj))
#         data = dc_pack(obj)
#         # header = dc_pack(SerializeHeader(np.uint32(len(data))))
#         writer.send(np.uint32(len(data)).tobytes() + data)

#     @classmethod
#     def unpack_data(cls, reader):
#         # return reader.recv()
#         l = int(np.frombuffer(recvall(reader, 4), np.uint32))
#         return msgpack.unpackb(recvall(reader, l))

# HEADER_SIZE = len(dc_pack(SerializeHeader(np.uint32(np.iinfo(np.uint32).max))))

# print("debug header size:", HEADER_SIZE)
