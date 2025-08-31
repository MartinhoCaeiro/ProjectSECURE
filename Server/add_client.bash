#!/bin/bash

# Verificar se foi passado o nome do cliente
if [ -z "$1" ]; then
    echo "Uso: $0 NOME_DO_CLIENTE"
    exit 1
fi

CLIENTE=$1
WG_CONF="/etc/wireguard/wg0.conf"
DIR_KEYS="./keys"
DIR_CLIENTES="./clientes"
IP_BASE="10.0.0."
SERVER_PUBLIC_KEY="vnk2SFpy/c1rPPDXzGSvbpVud0VGOXTABzS0swik2UU="
SERVER_ENDPOINT="85.246.65.217:51820"

# Criar diretórios se não existirem
mkdir -p "$DIR_KEYS" "$DIR_CLIENTES"

# Gerar chaves do cliente
CLIENT_PRIV=$(wg genkey)
CLIENT_PUB=$(echo "$CLIENT_PRIV" | wg pubkey)

# Guardar chaves (opcional)
echo "$CLIENT_PRIV" > "${DIR_KEYS}/${CLIENTE}_private.key"
echo "$CLIENT_PUB" > "${DIR_KEYS}/${CLIENTE}_public.key"

# Descobrir o próximo IP disponível
ULTIMO_IP=$(grep AllowedIPs "$WG_CONF" | awk -F'[./]' '{print $4}' | sort -n | tail -1)
if [ -z "$ULTIMO_IP" ]; then
    NOVO_IP="2"
else
    NOVO_IP=$((ULTIMO_IP + 1))
fi
CLIENT_IP="${IP_BASE}${NOVO_IP}/32"

# Criar ficheiro de configuração do cliente
cat > "${DIR_CLIENTES}/${CLIENTE}_INFO.conf" <<EOF
[Interface]
PrivateKey = ${CLIENT_PRIV}
Address = ${IP_BASE}${NOVO_IP}/32
DNS = 8.8.8.8

[Peer]
PublicKey = ${SERVER_PUBLIC_KEY}
Endpoint = ${SERVER_ENDPOINT}
AllowedIPs = 10.0.0.0/24
PersistentKeepalive = 25
EOF

# Adicionar o cliente ao wg0.conf do servidor
wg-quick down wg0

cat >> "$WG_CONF" <<EOF

# ${CLIENTE}
[Peer]
PublicKey = ${CLIENT_PUB}
AllowedIPs = ${CLIENT_IP}
EOF


wg-quick up wg0
# Mostrar resultado
echo "✔ Cliente '$CLIENTE' criado com IP ${IP_BASE}${NOVO_IP}"
echo "Ficheiro de configuração: ${DIR_CLIENTES}/${CLIENTE}_INFO.conf"
echo "Peer adicionado ao wg0.conf"







