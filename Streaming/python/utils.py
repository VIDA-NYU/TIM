import contextlib


@contextlib.contextmanager
def server(HOST, PORT):
    '''A simple server socket - for sending data.'''
    import socket
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            print(f'Listening: {HOST}:{PORT}')
            s.bind((HOST, PORT))
            s.listen()
            print("Awaiting connection...")
            c, addr = s.accept()
            with c:
                try:
                    print('Connected:', addr)
                    yield c
                finally:
                    if not c._closed:
                        try:
                            c.shutdown(socket.SHUT_RDWR)
                        except OSError:
                            pass
    except BrokenPipeError:
        print("Broken Pipe")
    except KeyboardInterrupt:
        print("\n\nInterrupted.")

@contextlib.contextmanager
def server_connection(HOST, PORT):
    '''A simple server socket - This will continuously accept connections.'''
    import socket
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            print(f'Listening: {HOST}:{PORT}')
            s.bind((HOST, PORT))
            s.listen()
            @contextlib.contextmanager
            def open():
                print("Awaiting connection...")
                c, addr = s.accept()
                with c:
                    try:
                        print('Connected:', addr)
                        yield c
                    finally:
                        if not c._closed:
                            try:
                                c.shutdown(socket.SHUT_RDWR)
                            except OSError:
                                pass
            yield open
    except KeyboardInterrupt:
        print("\n\nInterrupted.")

# @contextlib.contextmanager
# def client_connections(HOST, PORT, retry=True, delay=3):
#     '''A simple server socket - This will continuously accept connections.'''
#     import socket
#     try:
#         while True:
#             try:
#                 with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
#                     print(f'Connecting: {HOST}:{PORT}')
#                     s.connect((HOST, PORT))
#                     print(f'Connected: {HOST}:{PORT}')
#                     yield s
#             except Exception:  # TODO exact exception
#                 print("Connection dropped.")
#                 time.sleep(delay)
        
#     except KeyboardInterrupt:
#         print("\n\nInterrupted.")
    

@contextlib.contextmanager
def client(HOST, PORT):
    '''A simple client socket - for receiving data.'''
    import socket
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            print(f'Connecting: {HOST}:{PORT}')
            s.connect((HOST, PORT))
            print(f'Connected: {HOST}:{PORT}')
            yield s
    except KeyboardInterrupt:
        import traceback
        traceback.print_exc()
        print("\n\nClosed.")


@contextlib.contextmanager
def zserver(HOST, PORT):
    '''A simple server socket - for sending data.'''
    import zmq
    context = zmq.Context()
    socket = context.socket(zmq.REP)
    try:
        socket.bind(f"tcp://{HOST}:{PORT}")
        yield socket
    except KeyboardInterrupt:
        socket.close()
        print("\n\nInterrupted.")
    

@contextlib.contextmanager
def zclient(HOST, PORT):
    '''A simple client socket - for receiving data.'''
    import zmq
    context = zmq.Context()
    socket = context.socket(zmq.REQ)
    try:
        socket.connect(f"tcp://{HOST}:{PORT}")
        yield socket
    except KeyboardInterrupt:
        import traceback
        traceback.print_exc()
        print("\n\nClosed.")




def recvall(sock, size, chunk=4096):
    data = bytearray()
    while len(data) < size:
        packet = sock.recv(min(chunk, size - len(data)))
        if not packet:  # Important!!
            break
        data.extend(packet)
    if len(data) < size:
        raise ValueError("Socket didn't return a full message")
    return bytes(data)
