let gameState = null;
let currentMode = 'ai'; // 'ai', 'campaign', 'pvp', 'online'
let currentShipToPlace = null;
let isHorizontal = true;
let availableShips = [];
let activePowerUp = null; // 'Sonar', 'Airstrike', 'DepthBomb'
let hubConnection = null;
let roomCode = null;
let selectedCampaignLevel = 1;

document.addEventListener('DOMContentLoaded', () => {
    document.addEventListener('keydown', (e) => {
        if (e.code === 'Space' && gameState && gameState.gameStatus === 'setup') {
            e.preventDefault();
            toggleOrientation();
        }
    });

    if (typeof signalR !== 'undefined') {
        initSignalR();
    }
});

function toggleVoice() {
    const isEnabled = voice.toggle();
    const btn = document.getElementById('btnVoiceToggle');
    btn.innerText = isEnabled ? "🔊 Voces: ON" : "🔇 Voces: OFF";
    if (isEnabled) voice.speak("Voces de tripulación activadas.");
}

function setMode(mode) {
    currentMode = mode;
    document.getElementById('btnVsAi').classList.toggle('active', mode === 'ai');
    document.getElementById('btnVsCampaign').classList.toggle('active', mode === 'campaign');
    document.getElementById('btnVsPvp').classList.toggle('active', mode === 'pvp');
    document.getElementById('btnVsOnline').classList.toggle('active', mode === 'online');

    document.getElementById('difficultyGroup').style.display = mode === 'ai' ? 'block' : 'none';
    document.getElementById('mapGroup').style.display = mode === 'ai' ? 'block' : 'none';
    document.getElementById('roomCodeGroup').style.display = mode === 'online' ? 'block' : 'none';
}

async function startGame() {
    sounds.playSonar();
    voice.speak("Iniciando operaciones navales.");
    const p1Name = document.getElementById('p1NameInput').value.trim() || "Comandante Alfa";
    const difficulty = document.getElementById('difficultySelect').value;
    const mapType = document.getElementById('mapTypeSelect').value;

    if (currentMode === 'online') {
        const inputCode = document.getElementById('roomCodeInput').value.trim().toUpperCase();
        if (inputCode.length === 4) {
            roomCode = inputCode;
            if (hubConnection) hubConnection.invoke("JoinRoom", roomCode, p1Name);
        } else {
            if (hubConnection) hubConnection.invoke("CreateRoom", p1Name);
        }
        return;
    }

    try {
        const response = await fetch('/api/game/start', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                vsAi: currentMode === 'ai' || currentMode === 'campaign',
                difficulty: difficulty,
                mapType: mapType,
                campaignLevel: currentMode === 'campaign' ? selectedCampaignLevel : 0,
                player1Name: p1Name,
                player2Name: "Rival Beta"
            })
        });
        gameState = await response.json();
        
        document.getElementById('startModal').style.display = 'none';
        document.getElementById('headerStats').style.display = 'flex';
        document.getElementById('logTerminal').style.display = 'block';

        availableShips = gameState.availableShips;
        if (availableShips.length > 0) currentShipToPlace = availableShips[0];

        renderUI();
        addLogLine(`Operación naval iniciada (${gameState.mapType || 'Estándar'}). Fase de posicionamiento activada.`, "info");
    } catch (err) {
        console.error("Error al iniciar juego:", err);
    }
}

function selectPowerUp(type) {
    if (!gameState || gameState.gameStatus !== 'playing') return;

    if (activePowerUp === type) {
        activePowerUp = null;
    } else {
        activePowerUp = type;
        sounds.playSonar();
        if (type === 'Sonar') voice.speakSonar();
        else if (type === 'Airstrike') voice.speakAirstrike();
        else if (type === 'DepthBomb') voice.speakDepthBomb();
    }

    updatePowerUpButtons();
}

function updatePowerUpButtons() {
    const pUps = gameState ? gameState.powerUps : null;
    if (!pUps) return;

    const btnSonar = document.getElementById('btnPowerupSonar');
    const btnAir = document.getElementById('btnPowerupAirstrike');
    const btnDepth = document.getElementById('btnPowerupDepthBomb');

    btnSonar.classList.toggle('active', activePowerUp === 'Sonar');
    btnSonar.classList.toggle('disabled', !pUps.sonarAvailable);

    btnAir.classList.toggle('active', activePowerUp === 'Airstrike');
    btnAir.classList.toggle('disabled', !pUps.airstrikeAvailable);

    btnDepth.classList.toggle('active', activePowerUp === 'DepthBomb');
    btnDepth.classList.toggle('disabled', !pUps.depthBombAvailable);
}

function toggleOrientation() {
    isHorizontal = !isHorizontal;
    document.getElementById('orientationLabel').innerText = isHorizontal ? "HORIZONTAL (H)" : "VERTICAL (V)";
    sounds.playSonar();
}

async function autoPlaceFleet() {
    try {
        const response = await fetch('/api/game/auto-place', { method: 'POST' });
        gameState = await response.json();
        sounds.playSonar();
        addLogLine("Flota desplegada automáticamente de forma táctica.", "info");
        renderUI();
    } catch (err) {
        console.error("Error en autocolocación:", err);
    }
}

async function placeShipManual(row, col) {
    if (!currentShipToPlace || !gameState || gameState.gameStatus !== 'setup') return;

    try {
        const response = await fetch('/api/game/place-ship', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                shipName: currentShipToPlace.Name,
                row: row,
                col: col,
                horizontal: isHorizontal
            })
        });

        if (!response.ok) {
            addLogLine("⚠️ Posición inválida para colocar el barco.", "miss");
            sounds.playMiss();
            return;
        }

        gameState = await response.json();
        sounds.playHit();
        addLogLine(`Barco ${currentShipToPlace.Name} posicionado.`, "sunk");

        const placedNames = gameState.player1.fleet.map(s => s.name);
        const nextShip = availableShips.find(s => !placedNames.includes(s.name));
        currentShipToPlace = nextShip || null;

        renderUI();
    } catch (err) {
        console.error("Error al colocar barco:", err);
    }
}

async function shootTile(row, col) {
    if (!gameState || gameState.gameStatus !== 'playing') return;

    if (activePowerUp) {
        executePowerUp(activePowerUp, row, col);
        return;
    }

    try {
        sounds.playMissile();
        const response = await fetch('/api/game/shoot', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ row: row, col: col })
        });

        if (!response.ok) return;

        const data = await response.json();
        gameState = data.state;

        if (data.playerShots) {
            data.playerShots.forEach(pShot => {
                processShotFeedback(pShot);
            });
        }

        if (data.bonusTurn) {
            addLogLine("⚡ ¡ HABILIDAD DE DESTRUCTOR RÁPIDO ! Obtienes 1 disparo adicional en este turno.", "powerup");
            voice.speak("¡Impacto en destructor enemigo! Disparo extra concedido.");
        } else if (data.aiShot) {
            setTimeout(() => {
                processShotFeedback(data.aiShot);
                renderUI();
            }, 600);
        }

        renderUI();

        if (gameState.gameStatus === 'gameover') {
            setTimeout(() => showVictoryModal(data.winner, data.achievements), 800);
        }
    } catch (err) {
        console.error("Error al disparar:", err);
    }
}

async function executePowerUp(type, row, col) {
    try {
        const response = await fetch('/api/game/use-powerup', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                type: type === 'Sonar' ? 1 : (type === 'Airstrike' ? 2 : 3),
                targetRow: row,
                targetCol: col,
                horizontal: isHorizontal
            })
        });

        if (!response.ok) return;

        const data = await response.json();
        gameState = data.state;
        activePowerUp = null;

        if (type === 'Sonar') {
            sounds.playSonar();
            addLogLine(`📡 ${data.message}`, "powerup");
        } else if (type === 'Airstrike') {
            sounds.playAirstrike();
            addLogLine("🚀 ¡ RÁFAGA DE ATAQUE AÉREO DESPLEGADA !", "powerup");
        } else if (type === 'DepthBomb') {
            sounds.playDepthBomb();
            addLogLine("💣 ¡ BOMBA DE PROFUNDIDAD EXPLOTADA EN ZONA !", "powerup");
        }

        renderUI();
    } catch (err) {
        console.error("Error al usar power-up:", err);
    }
}

function processShotFeedback(shot) {
    const coordStr = `${String.fromCharCode(65 + shot.col)}${shot.row + 1}`;

    if (shot.result === 'Miss') {
        sounds.playMiss();
        voice.speakMiss();
        addLogLine(`🎯 ${shot.shooter} disparó a ${coordStr}: AGUA.`, "miss");
    } else if (shot.result === 'Hit') {
        sounds.playHit();
        voice.speakHit();
        addLogLine(`🔥 ${shot.shooter} disparó a ${coordStr}: ¡ IMPACTO DIRECTO !`, "hit");
    } else if (shot.result === 'Sunk') {
        sounds.playSunk();
        voice.speakSunk(shot.hitShipName);
        document.body.classList.add('shake');
        setTimeout(() => document.body.classList.remove('shake'), 500);
        addLogLine(`☠️ ${shot.shooter} hundió el ${shot.hitShipName || 'barco'} en ${coordStr}.`, "sunk");
    } else if (shot.result === 'MineHit') {
        sounds.playDepthBomb();
        voice.speakMine();
        addLogLine(`💣 ¡ ALERTA ! ${shot.shooter} detonó una mina naval oculta en ${coordStr}.`, "powerup");
    } else if (shot.result === 'IslandHit') {
        sounds.playMiss();
        addLogLine(`🏔️ Disparo a ${coordStr} rebotó en una isla de piedra.`, "miss");
    }
}

function renderUI() {
    if (!gameState) return;

    document.getElementById('turnIndicator').innerText = gameState.currentPlayer || "Comandante";
    const p1 = gameState.player1;
    const p2 = gameState.player2;

    if (p1) document.getElementById('accuracyStat').innerText = `${p1.accuracy}%`;
    if (p2) {
        const remaining = p2.fleet ? p2.fleet.filter(s => !s.isSunk).length : 5;
        document.getElementById('enemyShipsStat').innerText = `${remaining}/5`;
    }

    if (gameState.gameStatus === 'setup') {
        document.getElementById('setupPanel').style.display = 'block';
        document.getElementById('boardsContainer').style.display = 'flex';
        document.getElementById('powerupsBar').style.display = 'none';
        renderShipSelector();
    } else if (gameState.gameStatus === 'playing') {
        document.getElementById('setupPanel').style.display = 'none';
        document.getElementById('boardsContainer').style.display = 'flex';
        document.getElementById('powerupsBar').style.display = 'flex';
        updatePowerUpButtons();
    }

    renderBoard('myBoard', p1.grid, p1.shipNames, false, false);
    renderBoard('attackBoard', p2.grid, null, gameState.gameStatus === 'playing', true);

    renderFleetStatus('myFleetStatus', p1.fleet);
    renderFleetStatus('enemyFleetStatus', p2.fleet, true);
}

function renderShipSelector() {
    const container = document.getElementById('shipsList');
    container.innerHTML = '';
    const placedNames = gameState.player1.fleet.map(s => s.name);

    availableShips.forEach(ship => {
        const isPlaced = placedNames.includes(ship.Name);
        const isActive = currentShipToPlace && currentShipToPlace.Name === ship.Name;

        const div = document.createElement('div');
        div.className = `ship-item ${isPlaced ? 'placed' : ''} ${isActive ? 'active' : ''}`;
        div.innerHTML = `<span>🚢 ${ship.Name}</span> <small>(${ship.Size})</small>`;
        
        if (!isPlaced) {
            div.onclick = () => {
                currentShipToPlace = ship;
                sounds.playSonar();
                renderShipSelector();
            };
        }
        container.appendChild(div);
    });
}

function renderBoard(elementId, grid, shipNames, isInteractive, isRadar) {
    const container = document.getElementById(elementId);
    container.innerHTML = '';

    const emptyCorner = document.createElement('div');
    emptyCorner.className = 'cell cell-header';
    container.appendChild(emptyCorner);

    for (let c = 0; c < 10; c++) {
        const header = document.createElement('div');
        header.className = 'cell cell-header';
        header.innerText = String.fromCharCode(65 + c);
        container.appendChild(header);
    }

    for (let r = 0; r < 10; r++) {
        const rowHeader = document.createElement('div');
        rowHeader.className = 'cell cell-header';
        rowHeader.innerText = r + 1;
        container.appendChild(rowHeader);

        for (let c = 0; c < 10; c++) {
            const cell = document.createElement('div');
            const status = grid[r][c];

            cell.className = 'cell';

            if (status === 0) {
                cell.classList.add('cell-water');
                cell.innerText = '~';
            } else if (status === 1) {
                if (!isRadar) {
                    cell.classList.add('cell-ship');
                    const shipName = shipNames ? shipNames[r][c] : '';
                    cell.innerText = shipName ? shipName[0] : 'O';
                } else {
                    cell.classList.add('cell-water');
                    cell.innerText = '~';
                }
            } else if (status === 2) {
                cell.classList.add('cell-miss');
                cell.innerText = '*';
            } else if (status === 3) {
                cell.classList.add('cell-hit');
                cell.innerText = 'X';
            } else if (status === 4) {
                cell.classList.add('cell-sunk');
                cell.innerText = '#';
            } else if (status === 5) { // Island
                cell.classList.add('cell-island');
                cell.innerText = '▲';
            } else if (status === 6) { // Mine
                if (!isRadar) {
                    cell.classList.add('cell-mine');
                    cell.innerText = '💣';
                } else {
                    cell.classList.add('cell-water');
                    cell.innerText = '~';
                }
            }

            if (!isRadar && gameState.gameStatus === 'setup') {
                cell.onmouseenter = () => highlightPlacementPreview(r, c, true);
                cell.onmouseleave = () => highlightPlacementPreview(r, c, false);
                cell.onclick = () => placeShipManual(r, c);
            }

            if (isRadar && isInteractive && (status === 0 || status === 1 || status === 5 || status === 6)) {
                cell.onclick = () => shootTile(r, c);
            }

            container.appendChild(cell);
        }
    }
}

function highlightPlacementPreview(startRow, startCol, isHovering) {
    if (!currentShipToPlace || !gameState || gameState.gameStatus !== 'setup') return;

    const size = currentShipToPlace.Size;
    const cells = document.querySelectorAll('#myBoard .cell');

    for (let i = 0; i < size; i++) {
        const r = isHorizontal ? startRow : startRow + i;
        const c = isHorizontal ? startCol + i : startCol;

        if (r >= 0 && r < 10 && c >= 0 && c < 10) {
            const cellIndex = (r + 1) * 11 + (c + 1);
            const targetCell = cells[cellIndex];
            if (targetCell) {
                if (isHovering) {
                    targetCell.classList.add('cell-preview');
                } else {
                    targetCell.classList.remove('cell-preview');
                }
            }
        }
    }
}

function renderFleetStatus(elementId, fleet, hideUnrevealed = false) {
    const container = document.getElementById(elementId);
    container.innerHTML = '';
    if (!fleet) return;

    fleet.forEach(ship => {
        const badge = document.createElement('div');
        const isSunk = ship.isSunk;
        badge.className = `fleet-badge ${isSunk ? 'sunk' : ''}`;
        
        if (hideUnrevealed && !isSunk) {
            badge.innerText = `🚢 ??? (${ship.size})`;
        } else {
            badge.innerText = `🚢 ${ship.name} (${ship.hits}/${ship.size})`;
        }
        container.appendChild(badge);
    });
}

function addLogLine(message, type = "info") {
    const body = document.getElementById('logBody');
    const line = document.createElement('div');
    line.className = `log-line ${type}`;
    line.innerText = `> ${message}`;
    body.appendChild(line);
    body.scrollTop = body.scrollHeight;
}

async function openLeaderboard() {
    try {
        const lbRes = await fetch('/api/game/leaderboard');
        const entries = await lbRes.json();

        const achRes = await fetch('/api/game/achievements');
        const achievements = await achRes.json();

        const tbody = document.querySelector('#leaderboardTable tbody');
        tbody.innerHTML = '';
        entries.forEach(e => {
            const tr = document.createElement('tr');
            tr.innerHTML = `<td><strong>${e.playerName}</strong></td><td>${e.wins}</td><td>${e.losses}</td><td>${e.bestAccuracy}%</td><td>${e.bestTurns}</td>`;
            tbody.appendChild(tr);
        });

        const achGrid = document.getElementById('achievementsGrid');
        achGrid.innerHTML = '';
        achievements.forEach(a => {
            const div = document.createElement('div');
            div.className = `achievement-card ${a.isUnlocked ? 'unlocked' : ''}`;
            div.innerHTML = `<span class="achievement-icon">${a.icon}</span><div><div class="achievement-title">${a.title}</div><div class="achievement-desc">${a.description}</div></div>`;
            achGrid.appendChild(div);
        });

        document.getElementById('leaderboardModal').style.display = 'flex';
    } catch (err) {
        console.error("Error al cargar récords:", err);
    }
}

function closeLeaderboard() {
    document.getElementById('leaderboardModal').style.display = 'none';
}

function showVictoryModal(winnerName, achievements = []) {
    sounds.playVictory();
    voice.speak(`¡Atención! Victoria otorgada a ${winnerName}.`);
    const modal = document.getElementById('gameOverModal');
    document.getElementById('victoryTitle').innerText = `¡ VICTORIA : ${winnerName.toUpperCase()} !`;
    document.getElementById('victoryMessage').innerText = `Has demostrado una superioridad táctica naval aplastante.`;
    
    let achHtml = achievements && achievements.length > 0 ? `<p style="color: var(--yellow-warn); margin-top: 10px;">🏅 ¡LOGROS DESBLOQUEADOS: ${achievements.join(', ')}!</p>` : '';

    const summary = document.getElementById('finalStats');
    summary.innerHTML = `
        <p>🎯 Disparos totales: <strong>${gameState.player1.totalShots}</strong></p>
        <p>📊 Precisión final: <strong>${gameState.player1.accuracy}%</strong></p>
        ${achHtml}
    `;
    modal.style.display = 'flex';
}

function resetGame() {
    document.getElementById('gameOverModal').style.display = 'none';
    document.getElementById('startModal').style.display = 'block';
    document.getElementById('headerStats').style.display = 'none';
    document.getElementById('boardsContainer').style.display = 'none';
    document.getElementById('logTerminal').style.display = 'none';
    document.getElementById('setupPanel').style.display = 'none';
    document.getElementById('powerupsBar').style.display = 'none';
}

/* SignalR Multijugador Real-Time Client */
function initSignalR() {
    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/battle")
        .build();

    hubConnection.on("RoomCreated", (code, hostName) => {
        roomCode = code;
        addLogLine(`🌐 Sala creada con éxito. Código de Sala: ${code}. Comparte este código con tu rival.`, "powerup");
        alert(`¡Sala creada! Código de Sala para tu rival: ${code}`);
    });

    hubConnection.on("PlayerJoined", (hostName, guestName) => {
        addLogLine(`👥 ${guestName} se ha unido a la sala multijugador.`, "sunk");
    });

    hubConnection.on("Error", (msg) => {
        alert(msg);
    });

    hubConnection.start().catch(err => console.error("Error al conectar SignalR:", err));
}
