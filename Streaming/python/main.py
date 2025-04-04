import time
import contextlib

import cv2
import models
from utils import *
from detections import *
from frame_header import *
import runner


class FrameReader(runner.Block):
    def run(self, ip, port):
        while self.running:
            try:
                with client(ip, port) as reader:
                    for () in self.read():
                        yield FrameHeader.read(reader)
            except ConnectionRefusedError:
                print('Connection refused.')
                time.sleep(3)
        

class FrameDetector(runner.Block):
    def run(self, model=None):
        if not model:
            return
        model = models.get_model(model)
        for img, in self.read():
            yield FrameResults(model(img),[{"X":0.5,"Y":0.5,"FontSize":3,"Content":"foobarbaz"}])

class Debug(runner.Block):
    def run(self):
        for data, in self.read():
            data = json_dump(data)
            #print(len(data.Detections))
            print(data)
            yield data

class Procedure(runner.Block):

    def detect_object(self, attend_label, dets):    
        detection = dets and any([det.Box.Label == attend_label for det in dets.Detections])
        recog_time = time.time() if detection else 0
        return detection, recog_time

    def run(self, procedure):
        action_description = None
        detection = False
        delta = 0
        recog_time = time.time()
        attend = True
        for dets, in self.read():
            if not procedure and detection:
                action_description = 'DONE'
                dets.Texts = [{"X":0.5,"Y":0.5,"FontSize":3,"Content":action_description}]
                yield dets
                continue
            if not attend and delta < (time.time() - recog_time_new):
                action_description = None
                recog_time = recog_time_new
                attend = True
            if action_description == None:
                curr_step = procedure.pop(0)
                attended_object = curr_step['label']
                action_description = curr_step['action']
                delta = curr_step['delay']
            if attend:
                detection, recog_time_new = self.detect_object(attended_object, dets)
            if recog_time_new > recog_time:
                attend = False
            dets.Texts = [{"X":0.5,"Y":0.5,"FontSize":3,"Content":action_description, "Detected": detection}]
            yield dets


class DetectionWriter(runner.Block):
    def run(self, port):
        with server_connection('0.0.0.0', port) as accept:
            while self.running:
                with accept() as writer:
                    for data, in self.read():
                        print(len(data.Detections))
                        print(data)
                        yield write_data(writer, data)

class DetectionDrawer(runner.Block):
    def draw_frame(self, im, dets=None, winName=None):
        if dets is None:
            dets = self.last_dets
        else:
            self.last_dets = dets
        im = dets.draw_cv(im)
        cv2.imshow(winName, im[:,:,::-1])
        cv2.waitKey(1)

    def run(self, winName, wait_for_det=False):
        try:
            self.last_dets = FrameResults([],[])
            for xs in self.read(when=runner.first if not wait_for_det else all):
                self.draw_frame(*xs, winName=winName)
                yield 
        finally:
            cv2.destroyAllWindows()


procedure = [
    {'action':'grab an orange','label':'orange','delay':5},
    {'action':'slice the orange','label':'knife','delay':5},
    {'action':'squeeze in a wine glass','label':'wine glass','delay':5},
    {'action':'enjoy','label':'person','delay':5},
] 
# procedure = [
#     {'action':'grab a cup','label':'cup','delay':5},
#     {'action':'grab your phone','label':'cell phone','delay':5},
# ] 
def desktop(holo_ip='127.0.0.1', holo_port=SensorTypePort.PV, obj_port=23939, model='yolov3', draw=True, send=False, procedure=procedure, profile=False):
    if profile:
        runner.Block.profile = True

    with runner.Graph() as g:
        reader = FrameReader(holo_ip, holo_port)()
        detector = FrameDetector(model)(reader)
        reasoning = Procedure(procedure)(detector)
        if send:
            DetectionWriter(obj_port, is_process=True)(reasoning)
            # Debug()(reasoning)
        if draw:
            drawer = DetectionDrawer(is_process=True, winName='main')(reader, reasoning)
        # reader0 = FrameReader(holo_ip, 23943)()
        # detector0 = FrameDetector(model)(reader0)
        # if draw:
        #     drawer0 = DetectionDrawer(is_process=True, winName = 'gll')(reader0, detector0)
        # reader1 = FrameReader(holo_ip, 23944)()
        # detector1 = FrameDetector(model)(reader1)
        # if draw:
        #     drawer1 = DetectionDrawer(is_process=True, winName = 'glf')(reader1, detector1)
        # reader2 = FrameReader(holo_ip, 23945)()
        # detector2 = FrameDetector(model)(reader2)
        # if draw:
        #     drawer2 = DetectionDrawer(is_process=True, winName = 'grf')(reader2, detector2)
        # reader3 = FrameReader(holo_ip, 23946)()
        # detector3 = FrameDetector(model)(reader3)
        # if draw:
        #     drawer3 = DetectionDrawer(is_process=True, winName = 'grr')(reader3, detector3)
        # readerDepth = FrameReader(holo_ip, 23948)()
        # detectorDepth = FrameDetector(model)(readerDepth)
        # if draw:
        #     drawerDepth = DetectionDrawer(is_process=True, winName = 'depth')(readerDepth, detectorDepth)
    g.run()


# def desktop(holo_ip='127.0.0.1', holo_port=SensorTypePort.PV, obj_port=12345, model='tinyyolov2'):
#     '''This is the main desktop inference code. It takes frames from the hololens and sends back detections.
    
#     '''
#     model = models.get_model(model)

#     try:
#         with client(holo_ip, holo_port) as reader:
#             while True:
#                 im = FrameHeader.read(reader)
#                 im = models.letterbox_image(im, model.input_size)
#                 dets = model(im)
#                 for d in dets:
#                     im = d.draw_cv(im)
#                 cv2.imshow("holo-desktop", im)
#                 cv2.waitKey(1)
#     finally:
#         cv2.destroyAllWindows()



if __name__ == '__main__':
    import fire
    fire.Fire()
