### SERVER (Flask) - server.py

from flask import Flask, request, send_file
import os
from threading import Lock

app = Flask(__name__)
lock = Lock()
UPLOAD_FOLDER = "uploaded_dbs"
os.makedirs(UPLOAD_FOLDER, exist_ok=True)
DB_PATH = os.path.join(UPLOAD_FOLDER, "chat.db")

# Store IP of last sender to prevent re-sending to it
last_sender_ip = None

@app.route('/upload', methods=['POST'])
def upload_db():
    global last_sender_ip
    if 'file' not in request.files:
        return 'No file part', 400

    file = request.files['file']
    if file.filename == '':
        return 'No selected file', 400

    with lock:
        file.save(DB_PATH)
        last_sender_ip = request.remote_addr
        print(f"[INFO] DB received from {last_sender_ip}")

    return 'File uploaded', 200

@app.route('/download', methods=['GET'])
def download_db():
    global last_sender_ip
    requester_ip = request.remote_addr

    if not os.path.exists(DB_PATH):
        return 'No DB available yet', 404

    if requester_ip == last_sender_ip:
        return 'No new DB', 204  # No update if it's the sender

    return send_file(DB_PATH, as_attachment=True)

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
