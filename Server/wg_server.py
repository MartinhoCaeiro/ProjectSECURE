
from flask import Flask, send_file, request
import os
from datetime import datetime

app = Flask(__name__)

CONF_DIR = "./clientes"  # onde estão os ficheiros .conf

def log(msg):
    """Função simples para logging com timestamp"""
    print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] {msg}")

@app.route('/download/<user>', methods=['GET'])
def download_wg_conf(user):
    requester_ip = request.remote_addr
    filename = f"{user}_INFO.conf"
    filepath = os.path.join(CONF_DIR, filename)

    log(f"Pedido de download recebido de {requester_ip}")
    log(f"Utilizador pedido: {user}")
    log(f"Vai tentar procurar o ficheiro: {filepath}")

    if not os.path.exists(filepath):
        log(f"❌ Ficheiro não encontrado: {filepath}")
        return f'Config file for {user} not found', 404

    log(f"✅ Ficheiro encontrado, a enviar: {filepath}")
    return send_file(filepath, as_attachment=True)

if __name__ == '__main__':
    app.run(host='192.168.1.106', port=9595)



