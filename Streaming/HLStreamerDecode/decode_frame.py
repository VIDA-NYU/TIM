import requests
import numpy as np
from PIL import Image
import cv2
import matplotlib.pyplot as plt
import json

from collections import namedtuple

SensorTypeCls = namedtuple(
    'SensorType', 'PV DepthLT DepthAHAT GLL GLF GRF GRR Accel Gyro Mag numSensor Calibration')
SensorType = SensorTypeCls(*range(len(SensorTypeCls._fields)))

# Function to decode frames from Research Mode sensors
def load(data):
    VersionMajor = int(np.frombuffer(data[0:1], np.uint8))  # ReadByte
    FrameType = int(np.frombuffer(data[1:2], np.uint8))  # ReadUInt16
    timestamp = int(np.frombuffer(data[2:10], np.uint64))  # ReadUInt64
    width = int(np.frombuffer(data[10:14], np.uint32))  # ReadUInt32
    height = int(np.frombuffer(data[14:18], np.uint32))  # ReadUInt32
    PixelStride = int(np.frombuffer(data[18:22], np.uint32))  # ReadUInt32
    ExtraInfoSize = int(np.frombuffer(data[22:26], np.uint32))  # ReadUInt32
    
    headerSize = 26

    if FrameType in {SensorType.Accel, SensorType.Gyro, SensorType.Mag}:
        sensorData = np.frombuffer(data[headerSize:headerSize+width*height*PixelStride], dtype = np.float32).reshape((height, width))
        timestamps = np.frombuffer(data[headerSize+width*height*PixelStride:headerSize+width*height*PixelStride+ExtraInfoSize], dtype = np.uint64)
        timestamps = (timestamps - timestamps[0]) // 100 + timestamp

    elif FrameType in {SensorType.DepthLT}:
        depthIm = np.frombuffer(data[headerSize:headerSize+width*height*PixelStride], dtype = np.uint16).reshape((height, width))
        imageEndOffset = headerSize+width*height*PixelStride
        RigtoWorldtransform = np.frombuffer(data[imageEndOffset:imageEndOffset+64], dtype = np.float32).reshape((4,4)).T
        
        hasABImage = ExtraInfoSize > 4*16
        if hasABImage:
            abIm = cv2.imdecode(np.frombuffer(data[imageEndOffset+64:], dtype=np.uint8), cv2.IMREAD_UNCHANGED)
            
        # Visualize the frames
        cv2.imshow('depth', depthIm / depthIm.max())
        cv2.waitKey(1)
        cv2.imshow('active brightness', abIm)
        cv2.waitKey(1)
            
    elif FrameType in {SensorType.GLF, SensorType.GRR, SensorType.GRF, SensorType.GLL}:
        im = cv2.imdecode(np.frombuffer(data[headerSize:headerSize+PixelStride], dtype=np.uint8), cv2.IMREAD_UNCHANGED)
        if FrameType in {SensorType.GLF, SensorType.GRR}:
            im = np.rot90(im, -1)
        elif FrameType in {SensorType.GRF, SensorType.GLL}:
            im = np.rot90(im)
            
        imageEndOffset = headerSize+PixelStride
        RigtoWorldtransform = np.frombuffer(data[imageEndOffset:imageEndOffset+64], dtype = np.float32).reshape((4,4)).T

        # Visualize the frame
        cv2.imshow(SensorTypeCls._fields[FrameType], im)
        cv2.waitKey(1)
    
    elif FrameType in {SensorType.PV}:
        im = cv2.imdecode(np.frombuffer(data[headerSize:headerSize+PixelStride], dtype=np.uint8), cv2.IMREAD_UNCHANGED)
        im = cv2.cvtColor(im, cv2.COLOR_BGR2RGB)
            
        imageEndOffset = headerSize+PixelStride
        RigtoWorldtransform = np.frombuffer(data[imageEndOffset:imageEndOffset+64], dtype = np.float32).reshape((4,4)).T
        FocalLengthX = float(np.frombuffer(data[imageEndOffset+64:imageEndOffset+68], np.float32))
        FocalLengthY = float(np.frombuffer(data[imageEndOffset+68:imageEndOffset+72], np.float32))
        PrinciplePointX = float(np.frombuffer(data[imageEndOffset+72:imageEndOffset+76], np.float32))
        PrinciplePointY = float(np.frombuffer(data[imageEndOffset+76:imageEndOffset+80], np.float32))

        # Visualize the frame
        if FrameType in {SensorType.PV}:
            im = cv2.cvtColor(im, cv2.COLOR_RGB2BGR)
        cv2.imshow(SensorTypeCls._fields[FrameType], im)
        cv2.waitKey(1)
        
    elif FrameType in {SensorType.Calibration}:
        LUT = np.frombuffer(data[headerSize:headerSize+width*height*PixelStride], dtype = np.float32).reshape((-1,3))
        CamtoRigtransform = np.frombuffer(data[headerSize+width*height*PixelStride:headerSize+width*height*PixelStride+64], dtype = np.float32).reshape((4,4)).T

token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJzb25pYSIsImV4cCI6MTY2NDA1NTg1N30.LyyYwW1zxZnLAW0RfLlNyxpY1UPLwM7aoMkL2PPPQkw"
uri = "://<REPLACE_WITH_SERVER_ADDRESS>:7890"
header = {"Authorization":"Bearer "+token}

# Main camera 
stream = "/data/main"
load(requests.get("http" + uri + stream, headers=header).content)

# Grayscale cameras 
stream = "/data/gll"
load(requests.get("http" + uri + stream, headers=header).content)

stream = "/data/glf"
load(requests.get("http" + uri + stream, headers=header).content)

stream = "/data/grf"
load(requests.get("http" + uri + stream, headers=header).content)

stream = "/data/grr"
load(requests.get("http" + uri + stream, headers=header).content)

# Depth and Active Brightness images
stream = "/data/depthlt"
load(requests.get("http" + uri + stream, headers=header).content)

# Camera Calibration data

stream = "/data/gllCal"
load(requests.get("http" + uri + stream, headers=header).content)
stream = "/data/glfCal"
load(requests.get("http" + uri + stream, headers=header).content)
stream = "/data/grfCal"
load(requests.get("http" + uri + stream, headers=header).content)
stream = "/data/grrCal"
load(requests.get("http" + uri + stream, headers=header).content)
stream = "/data/depthltCal"
load(requests.get("http" + uri + stream, headers=header).content)

# IMU data
stream = "/data/imuaccel"
load(requests.get("http" + uri + stream, headers=header).content)

stream = "/data/imugyro"
load(requests.get("http" + uri + stream, headers=header).content)

stream = "/data/imumag"
load(requests.get("http" + uri + stream, headers=header).content)

# Hand Tracking data
stream = "/data/hand"
handData = json.loads(requests.get("http" + uri + stream, headers=header).content)
leftHand = json.loads(handData['left'])
rightHand = json.loads(handData['right'])

# Eye Tracking data (Timestamp is int64 from https://docs.microsoft.com/en-us/dotnet/api/system.datetime?view=net-6.0#persisting-values-as-integers)
stream = "/data/eye"
eyeData = json.loads(requests.get("http" + uri + stream, headers=header).content)