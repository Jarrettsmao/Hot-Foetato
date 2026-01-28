import { WebSocketServer, WebSocket } from 'ws';

const wss = new WebSocketServer({ port: 8080 });

wss.on('connection', (ws) => {
    console.log('Client connected');

    ws.on('message', (data) => {
        const message = JSON.parse(data.toString());

        ws.send(JSON.stringify({ type: 'ECHO', data: message }));
    });

    ws.on('close', () => {
        console.log('Client disconnected');
    });
});

