#if USE_DATA_CACHING
// The version only moves on a promotion, so every dev build would share one
// cache name. DATA_FILENAME is content-hashed (webGLNameFilesAsHashes), which
// gives each build its own cache and lets activate drop the previous ones.
const cacheName = {{{JSON.stringify(COMPANY_NAME + "-" + PRODUCT_NAME + "-" + PRODUCT_VERSION + "-" + DATA_FILENAME)}}};
const contentToCache = [
  "Build/{{{ LOADER_FILENAME }}}",
  "Build/{{{ FRAMEWORK_FILENAME }}}",
  #if USE_THREADS
  "Build/{{{ WORKER_FILENAME }}}",
  #endif
  "Build/{{{ DATA_FILENAME }}}",
  "Build/{{{ CODE_FILENAME }}}",
  "TemplateData/style.css"
];
#endif

self.addEventListener('install', (e) => {
  console.log('[Service Worker] Install');
  #if USE_DATA_CACHING
  e.waitUntil((async () => {
    const cache = await caches.open(cacheName);
    await cache.addAll(contentToCache.map(p => new Request(p, { cache: 'reload' })));
  })());
  self.skipWaiting();
  #endif
});

#if USE_DATA_CACHING
// Without this, caches from earlier builds stay forever and keep answering
// requests: an old favicon outlived the icon change because of it.
self.addEventListener('activate', (e) => {
  e.waitUntil((async () => {
    const names = await caches.keys();
    await Promise.all(names.filter(n => n !== cacheName).map(n => caches.delete(n)));
    await self.clients.claim();
  })());
});

async function fetchAndStore(req) {
  const resp = await fetch(req);
  if (resp.ok && resp.type !== 'opaque') {
    const cache = await caches.open(cacheName);
    await cache.put(req, resp.clone());
  }
  return resp;
}

// Build/ files carry a content hash in their name, so a cached copy is never stale.
async function cacheFirst(req) {
  const cache = await caches.open(cacheName);
  const hit = await cache.match(req);
  if (hit) return hit;
  try {
    return await fetchAndStore(req);
  } catch (err) {
    return new Response('', {status: 504, statusText: 'Gateway Timeout'});
  }
}

// Everything else keeps its name across builds (TemplateData/, icons, manifest),
// so ask the network first and fall back to the cache only when offline.
async function networkFirst(req) {
  try {
    return await fetchAndStore(req);
  } catch (err) {
    const cache = await caches.open(cacheName);
    const hit = await cache.match(req);
    return hit || new Response('', {status: 504, statusText: 'Gateway Timeout'});
  }
}

self.addEventListener('fetch', (e) => {
  const req = e.request;

  if (req.url.endsWith("index.html")) {
    e.respondWith(fetch(req));
    return;
  }

  if (req.method !== 'GET') return;

  const url = new URL(req.url);
  if (url.origin !== self.location.origin) return;

  if (url.pathname.includes('/Build/')) {
    e.respondWith(cacheFirst(req));
    return;
  }

  const isStaticAsset =
    url.pathname.includes('/TemplateData/') ||
    /\.(js|css|wasm|data|unityweb|png|jpg|jpeg|gif|webp|svg|ico|webmanifest)$/i.test(url.pathname);

  if (isStaticAsset) {
    e.respondWith(networkFirst(req));
  }
});
#endif