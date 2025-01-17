FROM docker.io/valkey/valkey:8.0.2 AS valkey

RUN echo '#!/bin/bash\n\
echo "bind ${VALKEY_BIND:-127.0.0.1}" >> /etc/valkey.conf\n\
echo "port ${VALKEY_PORT:-6379}" >> /etc/valkey.conf\n\
echo "protected-mode ${VALKEY_PROTECTED_MODE:-no}" >> /etc/valkey.conf\n\
echo "timeout ${VALKEY_TIMEOUT:-0}" >> /etc/valkey.conf\n\
echo "tcp-keepalive ${VALKEY_TCP_KEEPALIVE:-0}" >> /etc/valkey.conf\n\
echo "daemonize ${VALKEY_DAEMONIZE:-no}" >> /etc/valkey.conf\n\
echo "pidfile ${VALKEY_PIDFILE:-/var/run/valkey.pid}" >> /etc/valkey.conf\n\
echo "loglevel ${VALKEY_LOGLEVEL:-notice}" >> /etc/valkey.conf\n\
echo "databases ${VALKEY_DATABASES:-16}" >> /etc/valkey.conf\n\
\n\
if [ ! -z "$VALKEY_PASSWORD" ]; then\n\
    echo "requirepass $VALKEY_PASSWORD" >> /etc/valkey.conf\n\
fi\n\
\n\
echo "maxclients ${VALKEY_MAXCLIENTS:-10000}" >> /etc/valkey.conf\n\
\n\
if [ ! -z "$VALKEY_MAXMEMORY" ]; then\n\
    echo "maxmemory $VALKEY_MAXMEMORY" >> /etc/valkey.conf\n\
fi\n\
\n\
if [ ! -z "$VALKEY_SAVE" ]; then\n\
    echo "save $VALKEY_SAVE" >> /etc/valkey.conf\n\
fi\n\
\n\
echo "rdbcompression ${VALKEY_RDBCOMPRESSION:-yes}" >> /etc/valkey.conf\n\
echo "dbfilename ${VALKEY_DBFILENAME:-dump.rdb}" >> /etc/valkey.conf\n\
echo "dir ${VALKEY_DIR:-.}" >> /etc/valkey.conf\n\
\n\
if [ "${VALKEY_AOF_ENABLED:-no}" = "yes" ]; then\n\
    echo "appendonly yes" >> /etc/valkey.conf\n\
    echo "appendfilename ${VALKEY_APPENDFILENAME:-appendonly.aof}" >> /etc/valkey.conf\n\
    echo "appendfsync ${VALKEY_APPENDFSYNC:-everysec}" >> /etc/valkey.conf\n\
fi\n\
\n\
if [ ! -z "$VALKEY_REPLICAOF" ]; then\n\
    echo "replicaof $VALKEY_REPLICAOF" >> /etc/valkey.conf\n\
fi\n\
\n\
if [ "${VALKEY_CLUSTER_ENABLED:-no}" = "yes" ]; then\n\
    echo "cluster-enabled yes" >> /etc/valkey.conf\n\
    echo "cluster-port ${VALKEY_CLUSTER_PORT:-16379}" >> /etc/valkey.conf\n\
    echo "cluster-config-file ${VALKEY_CLUSTER_CONFIG_FILE:-nodes.conf}" >> /etc/valkey.conf\n\
    echo "cluster-node-timeout ${VALKEY_CLUSTER_NODE_TIMEOUT:-5000}" >> /etc/valkey.conf\n\
    echo "cluster-announce-ip ${VALKEY_CLUSTER_ANNOUNCE_IP:-0.0.0.0}" >> /etc/valkey.conf\n\
    echo "cluster-announce-port ${VALKEY_CLUSTER_ANNOUNCE_PORT:-6379}" >> /etc/valkey.conf\n\
    echo "cluster-announce-bus-port ${VALKEY_CLUSTER_ANNOUNCE_BUS_PORT:-16379}" >> /etc/valkey.conf\n\
fi\n\
\n\
valkey-server /etc/valkey.conf' >> /root/configurate.sh

RUN chmod +x /root/configurate.sh

WORKDIR /data

CMD ["/root/configurate.sh"]
