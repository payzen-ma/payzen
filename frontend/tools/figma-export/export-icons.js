#!/usr/bin/env node
"use strict";
const fs = require('fs');
const path = require('path');

const FILE_KEY = process.env.FIGMA_FILE_KEY || 'qyN1xWJHA33vgi7ne4DbeU';
const FRAME_NODE_ID = process.env.FIGMA_ICONS_NODE || '3321:278';
const TOKEN = process.env.FIGMA_TOKEN;
const OUT_DIR = path.resolve(process.cwd(), 'src', 'assets', 'icons');

if (!TOKEN) {
    console.error('Missing FIGMA_TOKEN environment variable.');
    console.error('Create a Personal Access Token in Figma and set: export FIGMA_TOKEN=xxxx');
    process.exit(1);
}

async function ensureFetch() {
    if (typeof fetch !== 'undefined') return fetch;
    // dynamic import for node <18
    const nf = await import('node-fetch');
    return nf.default;
}

function sanitizeName(name) {
    // remove Icon= prefix if present
    let base = name.replace(/^Icon[=:]?/i, '').trim();
    base = base.replace(/[^a-z0-9-_]/gi, '-');
    base = base.replace(/-+/g, '-');
    base = base.replace(/(^-|-$)/g, '');
    return base.toLowerCase() || 'icon';
}

async function fetchJson(url) {
    const _fetch = await ensureFetch();
    const res = await _fetch(url, { headers: { 'X-Figma-Token': TOKEN } });
    if (!res.ok) throw new Error(`Failed to fetch ${url}: ${res.status}`);
    return res.json();
}

async function fetchText(url) {
    const _fetch = await ensureFetch();
    const res = await _fetch(url);
    if (!res.ok) throw new Error(`Failed to fetch ${url}: ${res.status}`);
    return res.text();
}

async function collectNodes(frameId) {
    const url = `https://api.figma.com/v1/files/${FILE_KEY}/nodes?ids=${encodeURIComponent(frameId)}`;
    const json = await fetchJson(url);
    const node = json.nodes && json.nodes[frameId] && json.nodes[frameId].document;
    if (!node) throw new Error('Frame node not found in Figma file');

    const map = new Map();

    const walk = (n) => {
        if (!n) return;
        // include possible icon bearing node types
        if (n.type && ['VECTOR', 'GROUP', 'COMPONENT', 'FRAME', 'BOOLEAN_OPERATION', 'INSTANCE'].includes(n.type)) {
            if (n.name && n.id) map.set(n.id, n.name);
        }
        if (n.children && n.children.length) {
            for (const c of n.children) walk(c);
        }
    };

    walk(node);
    return Array.from(map.entries()).map(([id, name]) => ({ id, name }));
}

async function batchFetchImages(ids) {
    const chunks = [];
    const batchSize = 50; // Figma API safe batch
    for (let i = 0; i < ids.length; i += batchSize) chunks.push(ids.slice(i, i + batchSize));

    const results = {};
    for (const chunk of chunks) {
        const idsParam = chunk.join(',');
        const url = `https://api.figma.com/v1/images/${FILE_KEY}?ids=${encodeURIComponent(idsParam)}&format=svg`;
        const json = await fetchJson(url);
        Object.assign(results, json.images || {});
    }
    return results;
}

async function downloadIcons() {
    console.log('Collecting nodes from frame', FRAME_NODE_ID);
    const nodes = await collectNodes(FRAME_NODE_ID);
    if (!nodes.length) {
        console.warn('No icon-like nodes found under frame', FRAME_NODE_ID);
        return;
    }

    const ids = nodes.map(n => n.id);
    console.log(`Found ${ids.length} candidate nodes. Requesting SVG URLs...`);
    const images = await batchFetchImages(ids);

    if (!fs.existsSync(OUT_DIR)) fs.mkdirSync(OUT_DIR, { recursive: true });

    const nameCount = {};

    for (const node of nodes) {
        const url = images[node.id];
        if (!url) {
            console.warn('No export URL for node', node.id, node.name);
            continue;
        }

        let name = sanitizeName(node.name || node.id);
        // disambiguate duplicates
        nameCount[name] = (nameCount[name] || 0) + 1;
        if (nameCount[name] > 1) name = `${name}-${nameCount[name]}`;

        try {
            const svgText = await fetchText(url);
            const outPath = path.join(OUT_DIR, `${name}.svg`);
            fs.writeFileSync(outPath, svgText, 'utf8');
            console.log('Saved', outPath);
        } catch (err) {
            console.error('Failed to download', node.id, err.message);
        }
    }

    console.log('\nDone. Icons saved to', OUT_DIR);
    console.log('Tip: run `npm run figma:export-icons` with FIGMA_TOKEN set.');
}

downloadIcons().catch(err => {
    console.error('Error exporting icons:', err);
    process.exit(1);
});
