class VoiceEngine {
    constructor() {
        this.synth = window.speechSynthesis || null;
        this.enabled = true;
        this.voice = null;
        this.init();
    }

    init() {
        if (!this.synth) return;

        const loadVoices = () => {
            const voices = this.synth.getVoices();
            this.voice = voices.find(v => v.lang.startsWith('es')) || voices[0] || null;
        };

        loadVoices();
        if (this.synth.onvoiceschanged !== undefined) {
            this.synth.onvoiceschanged = loadVoices;
        }
    }

    speak(text) {
        if (!this.enabled || !this.synth) return;

        try {
            this.synth.cancel(); // Cancelar locución anterior
            const utterance = new SpeechSynthesisUtterance(text);
            if (this.voice) utterance.voice = this.voice;
            utterance.lang = 'es-ES';
            utterance.rate = 1.05;
            utterance.pitch = 0.95;
            this.synth.speak(utterance);
        } catch (e) {
            console.error("Error al sintetizar voz:", e);
        }
    }

    toggle() {
        this.enabled = !this.enabled;
        return this.enabled;
    }

    speakHit() {
        const phrases = [
            "¡Comandante, impacto directo en blanco enemigo!",
            "¡Blanco fijado! Impacto confirmado.",
            "¡Fuego certero, barco enemigo alcanzado!"
        ];
        this.speak(phrases[Math.floor(Math.random() * phrases.length)]);
    }

    speakMiss() {
        const phrases = [
            "Agua. Sin contactos en esa coordenada.",
            "Disparo al agua. Ajustando trayectorias.",
            "Sector limpio, sin barcos en la zona."
        ];
        this.speak(phrases[Math.floor(Math.random() * phrases.length)]);
    }

    speakSunk(shipName) {
        this.speak(`¡Atención! ${shipName || "Barco"} enemigo completamente destruido y hundido.`);
    }

    speakSonar() {
        this.speak("¡Sistemas de sonar activados! Escaneando sector.");
    }

    speakAirstrike() {
        this.speak("¡Ráfaga de ataque aéreo de misiles desplegada!");
    }

    speakDepthBomb() {
        this.speak("¡Bomba de profundidad detonada en la zona!");
    }

    speakMine() {
        this.speak("¡Alerta! Mina naval oculta detonada.");
    }
}

const voice = new VoiceEngine();
