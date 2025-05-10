const WebSocket = require('ws');

const socket = new WebSocket('ws://localhost:8080/');

socket.onopen = () => {
    console.log('Connected to server');
};

socket.onmessage = (event) => {
    console.log('Received message:', event.data);
};

socket.onclose = () => {
    console.log('Connection closed');
};

socket.onerror = (error) => {
    console.error('WebSocket error:', error);
};