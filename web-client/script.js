const ws = new WebSocket("ws://localhost:5000/ws");
const clientName = "web-" + Math.floor(Math.random() * 1000);
let selectedDevice = null;

// ================= CONNECT =================
ws.onopen = () => {
    ws.send(JSON.stringify({
        type: "register",
        clientType: "web",
        name: clientName
    }));
};

// ================= RECEIVE =================
ws.onmessage = (event) => {
    const msg = JSON.parse(event.data);

    // ===== DESKTOP LIST =====
    if (msg.type === "desktop_list") {
        renderDeviceList(msg.data);
    }

    // ===== RESPONSE =====
    if (msg.type === "response") {
        handleResponse(msg);
    }
};

// ================= RENDER SIDEBAR =================
function renderDeviceList(devices) {
    const container = document.getElementById("deviceList");
    container.innerHTML = "";

    devices.forEach(device => {
        const div = document.createElement("div");
        div.className = "device";
        div.innerText = device;

        div.onclick = () => {
            selectedDevice = device;

            // highlight selected
            document.querySelectorAll(".device").forEach(d => d.classList.remove("active"));
            div.classList.add("active");

            console.log("Selected:", device);
        };

        container.appendChild(div);
    });
}

// ================= SEND COMMAND =================
function sendCommand(command) {
    if (!selectedDevice) {
        alert("Please select a device");
        return;
    }

    ws.send(JSON.stringify({
        type: "command",
        from: clientName,
        to: selectedDevice,
        command: command
    }));
}

// ================= HANDLE RESPONSE =================
function handleResponse(msg) {
    const raw = atob(msg.data); // base64 → raw string

    // Example: display in active tab
    const activePanel = document.querySelector(".panel.active");

    if (activePanel.id === "screenshot") {
        // If it's image base64 → display image
        const img = document.createElement("img");
        img.src = "data:image/png;base64," + msg.data;
        img.style.maxWidth = "100%";

        activePanel.innerHTML = "";
        activePanel.appendChild(img);
    } else {
        activePanel.innerText = raw;
    }
}

// ================= TAB SWITCH =================
document.querySelectorAll(".tab").forEach(tab => {
    tab.onclick = () => {
        // switch tab UI
        document.querySelectorAll(".tab").forEach(t => t.classList.remove("active"));
        document.querySelectorAll(".panel").forEach(p => p.classList.remove("active"));

        tab.classList.add("active");
        const target = tab.dataset.tab;
        document.getElementById(target).classList.add("active");

        // send command based on tab
        sendCommand(target);
    };
});