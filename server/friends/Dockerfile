FROM python:3.12-slim
RUN useradd --create-home --uid 10001 myday && mkdir /data && chown myday:myday /data
WORKDIR /app
COPY friends_server.py .
USER myday
ENV HOST=0.0.0.0 PORT=8787 MYDAY_FRIENDS_DB=/data/friends.sqlite3 PYTHONUNBUFFERED=1
EXPOSE 8787
CMD ["python", "friends_server.py"]
