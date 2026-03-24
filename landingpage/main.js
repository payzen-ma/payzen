// PROGRESS BAR
window.addEventListener('scroll', () => {
    const p = window.scrollY / (document.body.scrollHeight - window.innerHeight) * 100;
    document.getElementById('pbar').style.width = p + '%';
    document.getElementById('nav').classList.toggle('s', window.scrollY > 20);
}, { passive: true });

// REVEAL ON SCROLL
const ob = new IntersectionObserver(es => {
    es.forEach(e => {
        if (e.isIntersecting) { e.target.classList.add('in'); ob.unobserve(e.target); }
    });
}, { threshold: .07, rootMargin: '0px 0px -36px 0px' });
document.querySelectorAll('.reveal').forEach(el => ob.observe(el));

// SMOOTH SCROLL
document.querySelectorAll('a[href^="#"]').forEach(a => a.addEventListener('click', e => {
    const t = document.querySelector(a.getAttribute('href'));
    if (t) { e.preventDefault(); t.scrollIntoView({ behavior: 'smooth' }); }
}));

// WAITLIST
window.join = function join(i, s, f) {
    console.log('[Waitlist] join() called — input id:', i, '| success id:', s, '| form id:', f);

    const el = document.getElementById(i);
    const v = el.value.trim();
    console.log('[Waitlist] Email entered:', v);

    if (!v || !v.includes('@') || !v.includes('.')) {
        console.warn('[Waitlist] Validation failed — email is empty or malformed:', v);
        el.style.borderColor = 'var(--orange)';
        el.style.boxShadow = '0 0 0 3px rgba(245,98,28,.1)';
        setTimeout(() => { el.style.borderColor = ''; el.style.boxShadow = ''; }, 1500);
        return;
    }

    const btn = el.nextElementSibling;
    const origTxt = btn.textContent;
    btn.disabled = true; btn.style.opacity = '.6'; btn.textContent = '...';

    const fd = new FormData();
    fd.append('action', 'subscribe');
    fd.append('email', v);
    fd.append('source', i === 'hem' ? 'hero' : 'cta');
    console.log('[Waitlist] Sending POST to ./subscribe.php', { action: 'subscribe', email: v, source: fd.get('source') });

    fetch('./subscribe.php', { method: 'POST', body: fd })
        .then(r => {
            console.log('[Waitlist] HTTP response — status:', r.status, r.statusText, '| ok:', r.ok);
            return r.ok
                ? r.json()
                : r.text().then(txt => {
                    console.error('[Waitlist] Non-OK response body:', txt);
                    return r.json().catch(() => ({ success: false, message: 'Erreur serveur.' }));
                });
        })
        .catch(err => {
            console.error('[Waitlist] Network/fetch error:', err);
            return { success: false, message: 'Erreur réseau. Réessayez.' };
        })
        .then(data => {
            console.log('[Waitlist] Parsed response data:', data);
            if (data.success) {
                console.log('[Waitlist] Success — hiding form, showing confirmation.');
                document.getElementById(f).style.display = 'none';
                document.getElementById(s).style.display = 'block';
            } else {
                console.warn('[Waitlist] Server returned success=false. Message:', data.message);
                el.style.borderColor = 'var(--orange)';
                el.style.boxShadow = '0 0 0 3px rgba(245,98,28,.1)';
                el.placeholder = data.message || 'Erreur. Réessayez.';
                setTimeout(() => { el.style.borderColor = ''; el.style.boxShadow = ''; el.placeholder = 'votre@email.com'; }, 3000);
            }
        })
        .finally(() => { btn.disabled = false; btn.style.opacity = ''; btn.textContent = origTxt; });
}

window.scrollCTA = function scrollCTA() {
    document.getElementById('cta').scrollIntoView({ behavior: 'smooth' });
    setTimeout(() => document.getElementById('cem').focus(), 600);
}

// AI MESSAGES
const msgs = [
    '<span class="ok">✓</span> Barèmes IR 2026 vérifiés et appliqués.',
    '<span class="ok">✓</span> Fichier <span class="hl">Damancom</span> prêt au dépôt.',
    '<span class="ok">✓</span> 47 bulletins générés et archivés.',
    '<span class="ok">✓</span> Cotisations CNSS Mars calculées — <span class="hl">38 450 MAD</span>.',
    '<span class="ok">✓</span> Déclaration AMO validée — conforme 2026.',
];
let mi = 1, di = 3;
setInterval(() => {
    const c = document.getElementById('aiMsgs');
    if (!c) return;
    const ex = c.querySelectorAll('.aim');
    if (ex.length >= 3) ex[0].remove();
    const m = document.createElement('div'); m.className = 'aim';
    m.innerHTML = `<div class="aim-av">AI</div><div class="aim-b">${msgs[mi % msgs.length]}</div>`;
    c.appendChild(m); mi++;
}, 3500);
setInterval(() => {
    const c = document.getElementById('daims');
    if (!c) return;
    const ex = c.querySelectorAll('.daim');
    if (ex.length >= 3) ex[0].remove();
    const m = document.createElement('div'); m.className = 'daim';
    m.innerHTML = `<div class="daim-av">AI</div><div class="daim-b">${msgs[di % msgs.length]}</div>`;
    c.appendChild(m); di++;
}, 4200);

// SIMULATOR
function fmt(n) { return Math.round(n).toLocaleString('fr-MA') + ' MAD'; }
window.calcSim = function calcSim() {
    const brut = parseFloat(document.getElementById('simSalaire').value) || 0;
    const anc = parseFloat(document.getElementById('simAnciennete').value) || 0;
    const enf = parseFloat(document.getElementById('simEnfants').value) || 0;
    const statut = document.getElementById('simStatut').value;

    let tauxAnc = 0;
    if (anc > 25) tauxAnc = .25;
    else if (anc > 20) tauxAnc = .2;
    else if (anc > 12) tauxAnc = .15;
    else if (anc > 5) tauxAnc = .1;
    else if (anc > 2) tauxAnc = .05;

    const primeAnc = brut * tauxAnc;
    const brutTotal = brut + primeAnc;
    const cnssBase = Math.min(brutTotal, 6000);
    const cnss = cnssBase * 0.0448;
    const amo = brutTotal * 0.0226;
    const fraisPro = Math.min(brutTotal * 0.2, 2500);
    const baseIR = brutTotal - cnss - amo - fraisPro;

    let chFam = 0;
    if (statut === 'marie') chFam = 360 / 12;
    chFam += enf * 30;

    const baseAnn = baseIR * 12;
    let irAnn = 0;
    if (baseAnn <= 30000) irAnn = 0;
    else if (baseAnn <= 50000) irAnn = (baseAnn - 30000) * 0.1;
    else if (baseAnn <= 60000) irAnn = 2000 + (baseAnn - 50000) * 0.2;
    else if (baseAnn <= 80000) irAnn = 4000 + (baseAnn - 60000) * 0.3;
    else if (baseAnn <= 180000) irAnn = 10000 + (baseAnn - 80000) * 0.34;
    else irAnn = 44000 + (baseAnn - 180000) * 0.38;

    const irMens = Math.max(0, (irAnn / 12) - chFam);
    const net = brutTotal - cnss - amo - irMens;
    const cnssP = cnssBase * 0.2109;
    const amoP = brutTotal * 0.0411;
    const coutTotal = brutTotal + cnssP + amoP;

    document.getElementById('rBrut').textContent = fmt(brut);
    document.getElementById('rAnciennete').textContent = '+ ' + fmt(primeAnc);
    document.getElementById('rCNSS').textContent = '– ' + fmt(cnss);
    document.getElementById('rAMO').textContent = '– ' + fmt(amo);
    document.getElementById('rBaseIR').textContent = fmt(baseIR);
    document.getElementById('rIR').textContent = '– ' + fmt(irMens);
    document.getElementById('rNet').textContent = fmt(net);
    document.getElementById('rCoutTotal').textContent = fmt(coutTotal);
    document.getElementById('cnssVal').textContent = fmt(cnss);
    document.getElementById('amoVal').textContent = fmt(amo);
    document.getElementById('cnssPatVal').textContent = fmt(cnssP);
    document.getElementById('amoPatVal').textContent = fmt(amoP);
}
calcSim();
