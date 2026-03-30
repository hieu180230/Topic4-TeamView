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
    console.log(msg);
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

    if (command === "PROCESS-KILL") {
        const pid = document.getElementById("pid").value;
        ws.send(JSON.stringify({
            type: "command",
            from: clientName,
            to: selectedDevice,
            command: command,
            pid: pid
        }));
    }

    else if (command === "PROCESS-START") {
        const pn = document.getElementById("pn").value;
        ws.send(JSON.stringify({
            type: "command",
            from: clientName,
            to: selectedDevice,
            command: command,
            pn: pn
        }));
    }

    else if (command === "APP-KILL") {
        const aid = document.getElementById("aid").value;
        ws.send(JSON.stringify({
            type: "command",
            from: clientName,
            to: selectedDevice,
            command: command,
            aid: aid
        }));
    }

    else if (command === "APP-START") {
        const an = document.getElementById("an").value;
        ws.send(JSON.stringify({
            type: "command",
            from: clientName,
            to: selectedDevice,
            command: command,
            an: an
        }));
    }

    else {
        ws.send(JSON.stringify({
            type: "command",
            from: clientName,
            to: selectedDevice,
            command: command
        }));
    }
}

// ================= HANDLE RESPONSE =================
function handleResponse(msg) {
    //const parsed = JSON.parse(msg.data); // WebSocket gives string
    console.log(msg);

    // Example: display in active tab
    const activePanel = document.querySelector(".panel.active");

    switch (activePanel.id) {
        case "keylog":
            renderKeylogTab(data);
            break;

        // case "shutdown":
        //     renderProcessTab(data);
        //     break;

        case "registry":
            //renderRegistryTab(data);
            break;

        case "screenshot":
            //renderScreenshotTab(data);
            break;

        case "process":
            if (msg.command === "process-kill") {
                document.getElementById("process-msg").textContent = msg.data;
            }
            else if (msg.command === "process-start") {
                document.getElementById("process-msg").textContent = msg.data;
            }
            else {
                renderProcessTab(msg.data);
            }
            break;

        case "app":
            if (msg.command === "app-kill") {
                document.getElementById("app-msg").textContent = msg.data;
            }
            else if (msg.command === "app-start") {
                document.getElementById("app-msg").textContent = msg.data;
            }
            else {
                renderAppTab(msg.data);
            }
            break;

        default:
            return;
    }

    // if (activePanel.id === "screenshot") {
    //     // If it's image base64 → display image
    //     const img = document.createElement("img");
    //     img.src = "data:image/png;base64," + msg.data;
    //     img.style.maxWidth = "100%";

    //     activePanel.innerHTML = "";
    //     activePanel.appendChild(img);
    // } else {
    //     activePanel.innerText = raw;
    // }
}

function renderProcessTab(data) {
    document.getElementById("process-msg").innerHTML = "";

    const container = document.querySelector(".process-data");
    container.innerHTML = "";

    let html = `
        <table class="process-table">
            <thead>
                <tr>
                    <th>Process Name</th>
                    <th>Process ID</th>
                    <th>Threads Count</th>
                </tr>
            </thead>
            <tbody>
    `;

    data.forEach(p => {
        html += `
            <tr>
                <td>${p[0]}</td>
                <td>${p[1]}</td>
                <td>${p[2]}</td>
            </tr>
        `;
    });

    html += `
            </tbody>
        </table>
    `;

    container.innerHTML = html;
}

function renderAppTab(data) {
    document.getElementById("app-msg").innerHTML = "";

    const container = document.querySelector(".app-data");
    container.innerHTML = "";

    let html = `
        <table class="app-table">
            <thead>
                <tr>
                    <th>Application Name</th>
                    <th>Application ID</th>
                    <th>Threads Count</th>
                </tr>
            </thead>
            <tbody>
    `;

    data.forEach(p => {
        if (p != null) {
            html += `
                <tr>
                    <td>${p[0]}</td>
                    <td>${p[1]}</td>
                    <td>${p[2]}</td>
                </tr>
            `;
        }
    });

    html += `
            </tbody>
        </table>
    `;

    container.innerHTML = html;
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
        //sendCommand(target);
    };
});

document.addEventListener("click", function (e) {
    if (e.target.matches("button[data-command]")) {
        const cmd = e.target.dataset.command;
        sendCommand(cmd);
    }
});