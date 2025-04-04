import dataclasses
import numpy as np
from PIL import Image
import cv2 
from utils import recvall

from collections import namedtuple

# SensorTypeUnity { 
#     Undefined = -1, PhotoVideo = 0, 
#     ShortThrowToFDepth = 1, ShortThrowToFReflectivity = 2, LongThrowToFDepth = 3, LongThrowToFReflectivity = 4, 
#     VisibleLightLeftLeft = 5, VisibleLightLeftFront = 6, VisibleLightRightFront = 7, VisibleLightRightRight = 8}
SensorTypeCls = namedtuple(
    'SensorType', 'null PV ShortFD ShortFR LongFD LongFR VisLL VisLF VisRF VisRR Accel Gyro Mag')

SensorType = SensorTypeCls(*range(-1, len(SensorTypeCls._fields) - 1))
SensorTypePort = SensorTypeCls(
    None, 23940, 23941, 23942, 23947, 23948, 23943, 23944, 23945, 23946, 23949, 23950, 23951)  # from: SensorFrameStreamer




@dataclasses.dataclass
class FrameHeader:
    width: int = 0
    height: int = 0
    PixelStride: int = 0
    RowStride: int = 0
    FrameType: int = SensorType.null
    time: int = 0
    Cookie: int = 0x484c524d
    VersionMajor: int = 0x00
    VersionMinor: int = 0x01
    
    @classmethod
    def load(cls, reader):
        h = cls()
        h.Cookie = int(np.frombuffer(recvall(reader, 4), np.uint32))  # ReadUInt32
        h.VersionMajor = int(np.frombuffer(recvall(reader, 1), np.uint8))  # ReadByte
        h.VersionMinor = int(np.frombuffer(recvall(reader, 1), np.uint8))  # ReadByte
        h.FrameType = int(np.frombuffer(recvall(reader, 2), np.uint16))  # ReadUInt16
        h.time = int(np.frombuffer(recvall(reader, 8), np.uint64))  # ReadUInt64
        h.width = int(np.frombuffer(recvall(reader, 4), np.uint32))  # ReadUInt32
        h.height = int(np.frombuffer(recvall(reader, 4), np.uint32))  # ReadUInt32
        h.PixelStride = int(np.frombuffer(recvall(reader, 4), np.uint32))  # ReadUInt32
        h.RowStride = int(np.frombuffer(recvall(reader, 4), np.uint32))  # ReadUInt32
        return h

    def dump(self):
        return b''.join([
            np.uint32(self.Cookie).tobytes(),
            np.uint8(self.VersionMajor).tobytes(),
            np.uint8(self.VersionMinor).tobytes(),
            np.uint16(self.FrameType).tobytes(),
            np.uint64(self.time).tobytes(),
            np.uint32(self.width).tobytes(),
            np.uint32(self.height).tobytes(),
            np.uint32(self.PixelStride).tobytes(),
            np.uint32(self.RowStride).tobytes(),
        ])

    @classmethod
    def read(cls, reader):
        h, data = cls.read_bytes(reader)
        return cls.from_bytes(h, data)

    @classmethod
    def read_bytes(cls, reader):
        h = cls.load(reader)
        # data = reader.recv(h.height * h.RowStride)
        return h, recvall(reader, h.height * h.RowStride)

    @classmethod
    def from_bytes(cls, h, data):
        # https://pillow.readthedocs.io/en/stable/handbook/concepts.html
        # https://docs.microsoft.com/en-us/uwp/api/windows.graphics.imaging.bitmappixelformat?view=winrt-22000
        width_mod = 1
        t = h.FrameType
        if t in {SensorType.PV}:
            mode = 'L'  # NV12
        elif t in {SensorType.ShortFD, SensorType.ShortFR, SensorType.LongFD, SensorType.LongFR}:
            mode = 'L'  # Gray8 - (8-bit pixels, black and white)
        elif t in {SensorType.VisLL, SensorType.VisLF, SensorType.VisRF, SensorType.VisRR}:
            mode = 'L'  # Gray8 - (8-bit pixels, black and white)
        elif t in {SensorType.Accel, SensorType.Gyro, SensorType.Mag}:
            mode = 'f'
        else:
            raise ValueError()

        if t in {SensorType.PV}:
            im = np.array(Image.frombytes(mode, (h.width, h.height), data))
            im = im[:,:-8]
            im = cv2.cvtColor(im, cv2.COLOR_YUV2RGB_NV12)
        elif t in {SensorType.VisLF, SensorType.VisRR}:
            im = np.array(Image.frombytes(mode, (h.width, h.height), data))
            im = np.rot90(im, -1)
            im = cv2.cvtColor(im, cv2.COLOR_GRAY2RGB)
        elif t in {SensorType.VisRF, SensorType.VisLL}:
            im = np.array(Image.frombytes(mode, (h.width, h.height), data))
            im = np.rot90(im)
            im = cv2.cvtColor(im, cv2.COLOR_GRAY2RGB)
        elif t in {SensorType.Accel, SensorType.Gyro, SensorType.Mag}:
            im = np.frombuffer(data, dtype=mode) 
        elif t in {SensorType.ShortFD, SensorType.ShortFR, SensorType.LongFD, SensorType.LongFR}:
            im = np.array(Image.frombytes(mode, (h.width, h.height), data))
            im = cv2.cvtColor(im, cv2.COLOR_GRAY2RGB)
        return im

    @classmethod
    def write(cls, writer, im, frame_type=SensorType.PV):
        h, w = im.shape[:2]
        if frame_type in {SensorType.PV}:
            pix_stride = 1
        elif frame_type in {SensorType.ShortFD, SensorType.LongFD}:
            pix_stride = 2
        elif frame_type in {SensorType.ShortFR, SensorType.LongFR, SensorType.VisLL, SensorType.VisLF, SensorType.VisRF, SensorType.VisRR}:
            pix_stride = 1
        else:
            raise ValueError()
        head = cls(w, h, pix_stride, w * pix_stride, frame_type)
        data = Image.fromarray(im).tobytes('raw', 'L', 0, 1)
        writer.sendall(head.dump())
        writer.sendall(data)
