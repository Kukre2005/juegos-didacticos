class VFXEngine {
    constructor() {
        this.canvas = null;
        this.ctx = null;
        this.particles = [];
        this.missiles = [];
        this.animId = null;
    }

    init() {
        this.canvas = document.getElementById('vfxCanvas');
        if (!this.canvas) {
            this.canvas = document.createElement('canvas');
            this.canvas.id = 'vfxCanvas';
            this.canvas.style.position = 'fixed';
            this.canvas.style.top = '0';
            this.canvas.style.left = '0';
            this.canvas.style.width = '100vw';
            this.canvas.style.height = '100vh';
            this.canvas.style.pointerEvents = 'none';
            this.canvas.style.zIndex = '90';
            document.body.appendChild(this.canvas);
        }

        this.ctx = this.canvas.getContext('2d');
        this.resize();
        window.addEventListener('resize', () => this.resize());
        this.loop();
    }

    resize() {
        if (this.canvas) {
            this.canvas.width = window.innerWidth;
            this.canvas.height = window.innerHeight;
        }
    }

    createExplosion(x, y) {
        const count = 35;
        for (let i = 0; i < count; i++) {
            const angle = Math.random() * Math.PI * 2;
            const speed = Math.random() * 6 + 2;
            this.particles.push({
                x: x,
                y: y,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                radius: Math.random() * 4 + 2,
                color: Math.random() > 0.3 ? '#ff0055' : (Math.random() > 0.5 ? '#ffbe0b' : '#ff5400'),
                alpha: 1,
                decay: Math.random() * 0.03 + 0.015
            });
        }
    }

    createWaterSplash(x, y) {
        const count = 20;
        for (let i = 0; i < count; i++) {
            const angle = Math.random() * Math.PI * 2;
            const speed = Math.random() * 4 + 1;
            this.particles.push({
                x: x,
                y: y,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed - 2, // ligera elevación
                radius: Math.random() * 3 + 2,
                color: Math.random() > 0.5 ? '#00f5d4' : '#0077b6',
                alpha: 1,
                decay: Math.random() * 0.04 + 0.02
            });
        }
    }

    launchMissile(startX, startY, targetX, targetY, onImpact) {
        this.missiles.push({
            x: startX,
            y: startY,
            startX: startX,
            startY: startY,
            targetX: targetX,
            targetY: targetY,
            progress: 0,
            speed: 0.04,
            onImpact: onImpact
        });
    }

    loop() {
        if (!this.ctx) return;
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

        // Actualizar partículas
        for (let i = this.particles.length - 1; i >= 0; i--) {
            const p = this.particles[i];
            p.x += p.vx;
            p.y += p.vy;
            p.alpha -= p.decay;

            if (p.alpha <= 0) {
                this.particles.splice(i, 1);
                continue;
            }

            this.ctx.save();
            this.ctx.globalAlpha = p.alpha;
            this.ctx.fillStyle = p.color;
            this.ctx.shadowBlur = 10;
            this.ctx.shadowColor = p.color;
            this.ctx.beginPath();
            this.ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
            this.ctx.fill();
            this.ctx.restore();
        }

        // Actualizar misiles
        for (let i = this.missiles.length - 1; i >= 0; i--) {
            const m = this.missiles[i];
            m.progress += m.speed;

            if (m.progress >= 1) {
                if (m.onImpact) m.onImpact();
                this.missiles.splice(i, 1);
                continue;
            }

            const currentX = m.startX + (m.targetX - m.startX) * m.progress;
            const arcHeight = Math.sin(m.progress * Math.PI) * -120;
            const currentY = m.startY + (m.targetY - m.startY) * m.progress + arcHeight;

            // Rastro de humo/fuego del misil
            this.particles.push({
                x: currentX,
                y: currentY,
                vx: (Math.random() - 0.5) * 1.5,
                vy: (Math.random() - 0.5) * 1.5,
                radius: Math.random() * 3 + 1,
                color: '#ffbe0b',
                alpha: 0.8,
                decay: 0.05
            });

            this.ctx.save();
            this.ctx.fillStyle = '#ffffff';
            this.ctx.shadowBlur = 15;
            this.ctx.shadowColor = '#00f5d4';
            this.ctx.beginPath();
            this.ctx.arc(currentX, currentY, 4, 0, Math.PI * 2);
            this.ctx.fill();
            this.ctx.restore();
        }

        this.animId = requestAnimationFrame(() => this.loop());
    }
}

const vfx = new VFXEngine();
document.addEventListener('DOMContentLoaded', () => vfx.init());
