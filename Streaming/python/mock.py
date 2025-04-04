import time
import cv2
import models
from utils import *
from detections import *
from frame_header import *
import runner
import main


def RGB2NV12(im):
    im = cv2.cvtColor(im, cv2.COLOR_BGR2YUV_I420)
    output = np.copy(im)
    UVStart = im.shape[0] // 3 * 2
    UVHeight = im.shape[0] // 3 // 2
    output[UVStart::2, ::2] = im[UVStart:UVStart+UVHeight, :im.shape[1] // 2]
    output[UVStart + 1::2, ::2] = im[UVStart:UVStart+UVHeight, im.shape[1] // 2:]
    output[UVStart::2, 1::2] = im[UVStart+UVHeight:UVStart+2*UVHeight, :im.shape[1] // 2]
    output[UVStart + 1::2, 1::2] = im[UVStart+UVHeight:UVStart+2*UVHeight, im.shape[1] // 2:]
    return output

class TestVideo(runner.Block):
    def run(self, src, connect_signal=None, fps=None):
        while self.running:
            if connect_signal:
                connect_signal.wait()
            cap = cv2.VideoCapture(src)
            ret, im = cap.read()
            im = RGB2NV12(im)
            if not ret:
                raise RuntimeError("No video data.")
            base_im = (np.ones(im.shape[:2])*255).astype(np.uint8)

            for () in self.read(fps=fps):
                ret, im = cap.read()
                im = RGB2NV12(im)
                if not ret:
                    break
                base_im[:,:] = im.astype(np.uint8)
                yield base_im
            print("Finished video stream.")

# class FrameWriter(runner.Block):
#     def run(self, holo_port, connect_signal=None):
#         while self.running:
#             if connect_signal:
#                 connect_signal.clear()
#             with server('0.0.0.0', holo_port) as writer:
#                 if connect_signal:
#                     connect_signal.set()
#                 for im, in self.read():
#                     yield FrameHeader.write(writer, im)


class FrameWriter(runner.Block):
    def run(self, port, connect_signal=None):
        if connect_signal:
            connect_signal.clear()
        with server_connection('0.0.0.0', port) as accept:
            if connect_signal:
                connect_signal.set()
            while self.running:
                with accept() as writer:                    
                    for im, in self.read():
                        yield FrameHeader.write(writer, im)


class DetectionReader(runner.Block):
    def run(self, ip, port):
        while self.running:
            try:
                with client(ip, port) as reader:
                    for () in self.read():
                        yield FrameResults.from_items(**read_data(reader))
            except ConnectionRefusedError:
                print('Connection refused.')
                time.sleep(3)


class FrameDetector(runner.Block):
    def run(self, model=None):
        if not model:
            return
        model = models.get_model(model)
        for img, in self.read():
            yield model(img)



def holo(src=1, holo_port=SensorTypePort.PV, obj_host='127.0.0.1', obj_port=12345, fps=18, profile=False):
    if profile:
        runner.Block.profile = True

    import threading
    ready = threading.Event()
    with runner.Graph() as g:
        vid = TestVideo(src, ready, fps=fps)
        write = FrameWriter(holo_port, ready)(vid)

        # def dets_dbg_msg(dets):
        #     seen_labels = {d.box.label for d in dets}
        #     return f'{len(dets)} detections. Saw: {seen_labels}'
        # dets = DetectionReader(obj_host, obj_port)
        # dbg = runner.Debug(dets_dbg_msg)(dets)
    g.run()



def draw(size=(400, 600), obj_host='127.0.0.1', obj_port=23939, profile=False):
    if profile:
        runner.Block.profile = True

    with runner.Graph() as g:
        dets = DetectionReader(obj_host, obj_port)
        vid = runner.Constant(np.zeros(size + (3,)), fps=30)
        drawer = main.DetectionDrawer(is_process=True, winName='holo-draw', wait_for_det=True)(vid, dets)

        def dets_dbg_msg(dets):
            seen_labels = {d.Box.Label for d in dets.Detections}
            return f'{len(dets.Detections)} detections. Saw: {seen_labels}'
        runner.Debug(dets_dbg_msg)(dets)
    g.run()


if __name__ == '__main__':
    import fire
    fire.Fire()
