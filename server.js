// Using express as a base for the server (compacter code)
const express = require("express");
// Using the OSC javascript implementation
const osc = require("osc");

// Create a server entity
const server = express();

// Set the data format for exchange between backend and frontend as JSON
server.use(express.json());
server.use(express.urlencoded({ extended: true }));

server.use("/", express.static(__dirname + "/static"));

// Setting the default port to 3000
server.set('port', process.env.PORT || 3000);

// Set the default page to display when entering the website
server.get('/', (request, response) => {
    response.sendFile(__dirname + '/index.html');
});

//Start the server and bind to http://localhost:3000
server.listen(3000, () => {
    console.log('Express server started at port 3000');
});

// An OSC object on UDP
let udpPort = new osc.UDPPort({});
// To store value for IP & Port of SC
let ip = 0, port = 0;

// Function to send an OSC message to SuperCollider
function sendMessage(response, message) {
    if (ip != 0 && port != 0) {
        // Close the existing connection
        udpPort.close();
        // Create a new object of udpPort
        udpPort = new osc.UDPPort({});
        // Open the UDP connection
        udpPort.open();
        // If the connection is ready => send message
        udpPort.on("ready", function () {
            udpPort.send(message, ip, port);
            response.json({ ok: true });
        });

        // Show debug info on the server
        console.log("Sending message", message.args, 
        "to", ip + ":" + port, "on", message.address);
    }
}

// When receiving valid IP and port from frontend
server.post('/ipSC', (request, response) => {
    // IP & Port where SC is listening for OSC messages
    ip = request.body.data[0];
    port = request.body.data[1];

    // Construct a "hello" message to send to SC
    var message = {
        address: '/hello',
        args: [
            {
                type: "s",
                value: "Hello from control board (web)!"
            }
        ]
    };

    // Send the message to SC
    sendMessage(response, message);
});

// When receiving demand to free everything in SC from frontend
server.post('/freeSC', (request, response) => {
    // Free resources in SC
    var message = {
        address: '/stopAll'
    };

    // Send the message to SC
    sendMessage(response, message);
});

// When a slider's value is changed on the page
server.post('/slider', (request, response) => {
    /*  Inside "request.body" is the data from the page, 
        which should be an array of [address, value] e.g. [x, 10] */
    var message = {
        address: '/x',
        args: [
            {
                type: "i",
                value: request.body.data[0]
            }
        ]
    };

    // Send the message to SC
    sendMessage(response, message);
});

server.post('/midi', (request, response) => {
    /* Inside "request.body" is the data from the page, 
    which should be an integer value for a midi note*/
    var message = {
        address: '/midi',
        args: [
            {
                type: "i",
                value: request.body.data
            }
        ]
    };

    // Send the message to SC
    sendMessage(response, message);
});

// When receiving info for ambisonic from frontend
server.post('/ambisonic', (request, response) => {
    /*  Inside "request.body" is the data from the page,
        if a sound is inside the big circle: data = [elementID, distance, azimuth] 
        if it's outside, data = [elementID] */
    if (request.body.data.length > 0) {
        var message = {
            // Address pattern to send to = "/ambisonic1" or "/ambisonic2"
            address: '/ambisonic' + request.body.data[0].slice(-1)
        };

        // If sound is inside, send distance and azimuth
        if (request.body.data.length == 3) {
            message.args = [
                {
                    type: "f",
                    value: request.body.data[1]
                },
                {
                    type: "f",
                    value: request.body.data[2]
                }];
        }
        // Send the message to SC
        sendMessage(response, message);
    }
});
