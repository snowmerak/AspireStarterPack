FROM docker.io/valkey/valkey:8.0.2 AS valkey

WORKDIR /root

RUN echo '"#!bin/sh\n" >> /root/create.sh \n\
if [ ! -z "$PASSWORD" ]; then \n\
    echo "valkey-cli --cluster create ${NODES} --cluster-replicas ${REPLICAS} --cluster-yes -a ${PASSWORD}" >> /root/create.sh \n\
else \n\
    echo "valkey-cli --cluster create ${NODES} --cluster-replicas ${REPLICAS} --cluster-yes" >> /root/create.sh \n\
fi \n\
chmod +x /root/create.sh \n\
cat /root/create.sh \n\
/root/create.sh' > /root/init.sh

RUN chmod +x /root/init.sh

CMD ["/bin/sh", "-c", "/root/init.sh"]
