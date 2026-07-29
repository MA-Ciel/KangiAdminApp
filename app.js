/* ============================================================
   app.js — Kangi NFT QR Manager
   Full flow: Login → Dashboard → Create NFT → Manage QRs → Redeem
   All data operations go through service.js → PlayFab CloudScript
   ============================================================ */
(function () {
  'use strict';

  /* ─── DOM shorthand ─── */
  const $ = (id) => document.getElementById(id);

  /* ─── Element refs ─── */
  const el = {
    /* Auth */
    loginView:      $('loginView'),
    loginForm:      $('loginForm'),
    loginEmail:     $('loginUsername'),
    loginPass:      $('loginPassword'),
    loginBtn:       $('loginBtn'),
    loginAlert:     $('loginAlert'),

    /* Shell */
    appView:        $('appView'),
    logoutBtn:      $('logoutBtn'),
    pageTitle:      $('pageTitle'),
    pageSub:        $('pageSubtitle'),
    sidebarUser:    $('sidebarUsername'),
    userAvatar:     $('userAvatar'),
    themeToggle:    $('themeToggle'),
    iconMoon:       $('iconMoon'),
    iconSun:        $('iconSun'),
    refreshBtn:     $('refreshBtn'),

    /* Dashboard */
    statNfts:       $('statNfts'),
    statCodes:      $('statCodes'),
    statRedeemed:   $('statRedeemed'),
    recentList:     $('recentNftsList'),

    /* Create */
    createAlert:    $('createAlert'),
    nftForm:        $('nftForm'),
    nftName:        $('nftName'),
    nftCount:       $('nftCount'),
    nftImage:       $('nftImage'),
    nftDesc:        $('nftDesc'),
    createBtn:      $('createNftBtn'),
    fileDrop:       $('fileDrop'),
    fileDropInner:  $('fileDropInner'),
    filePreview:    $('filePreview'),
    filePreviewWrap:$('filePreviewWrap'),
    filePreviewName:$('filePreviewName'),

    /* Manage */
    nftLibrary:     $('nftLibrary'),
    clearAllBtn:    $('clearAllBtn'),

    /* Redeem */
    redeemAlert:    $('redeemAlert'),
    redeemToken:    $('redeemToken'),
    redeemBtn:      $('redeemBtn'),
    redeemResult:   $('redeemResult'),

    /* Sounds */
    soundsLibrary:  $('soundsLibrary'),
    btnFilterAllSongs: $('btnFilterAllSongs'),
    btnFilterPendingSongs: $('btnFilterPendingSongs'),
    btnFilterApprovedSongs: $('btnFilterApprovedSongs'),
    soundsAlert:    $('soundsAlert')
  };

  /* ─── App state ─── */
  const state = { nfts: [], songs: [], songFilter: 'all' };

  /* ─── View metadata ─── */
  const VIEWS = {
    dashboard: { title: 'Dashboard',  sub: 'Overview of your NFT collection and activity' },
    create:    { title: 'Create NFT', sub: 'Upload an NFT image and generate unique QR codes' },
    manage:    { title: 'Manage QRs', sub: 'Browse all NFT batches, download QRs and copy redeem links' },
    sounds:    { title: 'Sounds Library', sub: 'Approve or delete audio files submitted to the server' },
    redeem:    { title: 'Redeem',     sub: 'Verify and process a one-time QR code redemption' }
  };

  /* ================================================================
     BOOT
     ================================================================ */
  function boot() {
    KangiService.init();
    _applyTheme(localStorage.getItem('kangi_theme') || 'dark');
    _bindLogin();
    _bindLogout();
    _bindNav();
    _bindCreate();
    _bindFileDrop();
    _bindSounds();
    _bindRedeem();
    _bindClearAll();
    _bindRefresh();
    _bindTheme();
  }

  /* ================================================================
     AUTH — Login with PlayFab Email + Password + isAdmin check
     ================================================================ */
  function _bindLogin() {
    /* Set email type & clear any prefilled values */
    el.loginEmail.type        = 'email';
    el.loginEmail.placeholder = 'Enter your email address';
    el.loginEmail.value       = '';
    el.loginPass.value        = '';

    /* Remove hint if present */
    const hint = el.loginView.querySelector('.auth-hint');
    if (hint) hint.remove();

    el.loginForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      const email    = el.loginEmail.value.trim();
      const password = el.loginPass.value;

      if (!email || !password) {
        return _alert(el.loginAlert, 'error', 'Please enter your email and password.');
      }

      _setLoading(el.loginBtn, true);
      _hideEl(el.loginAlert);

      try {
        const res = await KangiService.login(email, password);
        _onLoginSuccess(res);
      } catch (msg) {
        _alert(el.loginAlert, 'error', msg);
      } finally {
        _setLoading(el.loginBtn, false);
      }
    });
  }

  function _onLoginSuccess(res) {
    const name = res.displayName || res.email.split('@')[0];
    el.sidebarUser.textContent = name;
    if (el.userAvatar) el.userAvatar.textContent = name.charAt(0).toUpperCase();

    el.loginView.classList.add('hidden');
    el.appView.classList.remove('hidden');

    _switchView('dashboard');
    _loadAllData();
    _checkHashRedeem();
  }

  /* ================================================================
     LOGOUT
     ================================================================ */
  function _bindLogout() {
    el.logoutBtn.addEventListener('click', () => {
      KangiService.logout();
      state.nfts    = [];
      el.appView.classList.add('hidden');
      el.loginView.classList.remove('hidden');
      el.loginEmail.value = '';
      el.loginPass.value  = '';
      _hideEl(el.loginAlert);
      location.hash = '';
    });
  }

  /* ================================================================
     NAVIGATION
     ================================================================ */
  function _bindNav() {
    document.querySelectorAll('[data-view]').forEach(btn =>
      btn.addEventListener('click', () => _switchView(btn.dataset.view))
    );
    /* Quick action tiles on dashboard */
    document.querySelectorAll('[data-nav]').forEach(btn =>
      btn.addEventListener('click', () => _switchView(btn.dataset.nav))
    );
  }

  async function _switchView(name) {
    document.querySelectorAll('.view').forEach(v => {
      v.classList.remove('active');
      v.classList.remove('hidden');
    });
    document.querySelectorAll('.nav-item').forEach(b => b.classList.remove('active'));

    const view = $(`${name}View`);
    if (view) {
      view.classList.remove('hidden');
      view.classList.add('active');
    }

    const navBtn = document.querySelector(`.nav-item[data-view="${name}"]`);
    if (navBtn) navBtn.classList.add('active');

    const meta = VIEWS[name] || { title: name, sub: '' };
    el.pageTitle.textContent = meta.title;
    el.pageSub.textContent   = meta.sub;

    if (name === 'sounds') {
      await _loadSongsData();
    }
  }

  /* ================================================================
     DATA — Load from PlayFab via CloudScript
     ================================================================ */
  async function _loadAllData() {
    try {
      const res  = await KangiService.getNfts();
      state.nfts = (res && Array.isArray(res.nfts)) ? res.nfts : [];
    } catch (e) {
      console.error('[Kangi] Data load error:', e);
      state.nfts = [];
    }
    _renderAll();
  }

  function _renderAll() {
    _renderStats();
    _renderRecent();
    _renderLibrary();
  }

  /* ─── Stats ─── */
  function _renderStats() {
    const codes    = state.nfts.flatMap(n => n.codes || []);
    const redeemed = codes.filter(c => c.redeemed).length;
    _countUp(el.statNfts,     state.nfts.length);
    _countUp(el.statCodes,    codes.length);
    _countUp(el.statRedeemed, redeemed);
  }

  function _countUp(el, target) {
    const start = parseInt(el.textContent) || 0;
    if (start === target) return;
    const step = target > start ? 1 : -1;
    let cur = start;
    const timer = setInterval(() => {
      cur += step;
      el.textContent = cur;
      if (cur === target) clearInterval(timer);
    }, 40);
  }

  /* ─── Recent NFTs (dashboard) ─── */
  function _renderRecent() {
    if (!state.nfts.length) {
      el.recentList.innerHTML = _emptyHTML('No NFTs created yet');
      return;
    }
    el.recentList.innerHTML = state.nfts.slice(0, 5).map(nft => {
      const total    = (nft.codes || []).length;
      const redeemed = (nft.codes || []).filter(c => c.redeemed).length;
      const avail    = total - redeemed;
      return `
        <div class="recent-item">
          <img class="recent-thumb" src="${nft.image}" alt="${_esc(nft.name)}" />
          <div class="recent-info">
            <div class="recent-name">${_esc(nft.name)}</div>
            <div class="recent-sub">${total} QR codes · ${redeemed} redeemed</div>
          </div>
          <span class="chip ${avail > 0 ? 'chip--teal' : 'chip--red'}">${avail} left</span>
        </div>`;
    }).join('');
  }

  /* ─── NFT Library (Manage QRs) ─── */
  function _renderLibrary() {
    if (!state.nfts.length) {
      el.nftLibrary.innerHTML = `
        <div class="empty-state">
          ${_nftSvg()}
          <p>No NFTs in your library yet</p>
          <button class="btn btn-primary btn-sm" onclick="document.querySelector('[data-view=create]').click()">Create your first NFT</button>
        </div>`;
      return;
    }

    el.nftLibrary.innerHTML = '';
    state.nfts.forEach((nft, ni) => {
      const codes    = nft.codes || [];
      const redeemed = codes.filter(c => c.redeemed).length;
      const pct      = codes.length ? Math.round((redeemed / codes.length) * 100) : 0;

      const entry = document.createElement('div');
      entry.className = 'nft-entry';

      /* ── Header ── */
      const head = document.createElement('div');
      head.className = 'nft-entry-head';
      head.innerHTML = `
        <img class="nft-thumb" src="${nft.image}" alt="${_esc(nft.name)}" />
        <div class="nft-meta">
          <strong>${_esc(nft.name)}</strong>
          ${nft.description ? `<div class="nft-desc-text">${_esc(nft.description)}</div>` : ''}
          <div class="nft-meta-row">
            <span class="chip chip--purple">${codes.length} QR codes</span>
            <span class="chip chip--green">${redeemed} redeemed</span>
            <span class="chip chip--teal">${codes.length - redeemed} remaining</span>
          </div>
          <div class="progress-bar-wrap">
            <div class="progress-bar" style="width:${pct}%"></div>
          </div>
        </div>
        <svg viewBox="0 0 20 20" fill="currentColor" class="chevron" style="width:18px;height:18px;color:var(--text-3);flex-shrink:0;transition:transform .25s ease;">
          <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd"/>
        </svg>`;

      /* ── Body (QR rows) ── */
      const body = document.createElement('div');
      body.className = 'nft-entry-body';

      codes.forEach((code) => {
        const row = document.createElement('div');
        row.className = 'qr-row';
        row.innerHTML = `
          <div class="qr-box" id="qr-box-${_safeId(code.token)}"></div>
          <div class="qr-details">
            <div class="qr-serial">${_esc(code.serial)}</div>
            <div style="display:flex;align-items:center;gap:.4rem;margin-bottom:.35rem;">
              <span class="chip ${code.redeemed ? 'chip--red' : 'chip--green'}">
                ${code.redeemed ? '✓ Redeemed' : '● Available'}
              </span>
              ${code.redeemed && code.redeemedAt
                ? `<span class="qr-url">at ${new Date(code.redeemedAt).toLocaleString()}</span>`
                : ''}
            </div>
            <div class="qr-url" title="${_esc(code.url)}">${_esc(code.url)}</div>
            <div class="qr-actions-row">
              <button class="btn btn-ghost btn-sm" data-action="download" data-token="${code.token}">
                <svg viewBox="0 0 20 20" fill="currentColor" style="width:13px;height:13px;"><path fill-rule="evenodd" d="M3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm3.293-7.707a1 1 0 011.414 0L9 10.586V3a1 1 0 112 0v7.586l1.293-1.293a1 1 0 111.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z" clip-rule="evenodd"/></svg>
                Download QR
              </button>
              <button class="btn btn-ghost btn-sm" data-action="copy" data-url="${code.url}">
                <svg viewBox="0 0 20 20" fill="currentColor" style="width:13px;height:13px;"><path d="M8 3a1 1 0 011-1h2a1 1 0 110 2H9a1 1 0 01-1-1z"/><path d="M6 3a2 2 0 00-2 2v11a2 2 0 002 2h8a2 2 0 002-2V5a2 2 0 00-2-2 3 3 0 01-3 3H9a3 3 0 01-3-3z"/></svg>
                Copy Link
              </button>
              ${!code.redeemed ? `
              <button class="btn btn-ghost btn-sm" data-action="open-redeem" data-token="${code.token}">
                <svg viewBox="0 0 20 20" fill="currentColor" style="width:13px;height:13px;"><path fill-rule="evenodd" d="M4 4a2 2 0 00-2 2v4a2 2 0 002 2V6h10a2 2 0 00-2-2H4zm2 6a2 2 0 012-2h8a2 2 0 012 2v4a2 2 0 01-2 2H8a2 2 0 01-2-2v-4zm6 4a2 2 0 100-4 2 2 0 000 4z" clip-rule="evenodd"/></svg>
                Open Redeem
              </button>` : ''}
            </div>
          </div>`;
        body.appendChild(row);
      });

      entry.appendChild(head);
      entry.appendChild(body);
      el.nftLibrary.appendChild(entry);

      /* ── Accordion toggle ── */
      let qrsGenerated = false;
      head.addEventListener('click', () => {
        const isOpen = body.classList.toggle('open');
        const chev   = head.querySelector('.chevron');
        if (chev) chev.style.transform = isOpen ? 'rotate(180deg)' : 'rotate(0deg)';

        /* Generate QR codes lazily on first open */
        if (isOpen && !qrsGenerated) {
          qrsGenerated = true;
          codes.forEach(code => {
            const box = document.getElementById(`qr-box-${_safeId(code.token)}`);
            if (box && !box.querySelector('canvas, img')) {
              try {
                new QRCode(box, {
                  text:         code.url,
                  width:        110,
                  height:       110,
                  correctLevel: QRCode.CorrectLevel.H
                });
              } catch (err) {
                box.textContent = '—';
                console.warn('QR gen error:', err);
              }
            }
          });
        }
      });

      /* ── Action button delegation ── */
      body.addEventListener('click', async (e) => {
        const btn = e.target.closest('[data-action]');
        if (!btn) return;
        e.stopPropagation();

        const action = btn.dataset.action;

        if (action === 'download') {
          const box    = document.getElementById(`qr-box-${_safeId(btn.dataset.token)}`);
          const canvas = box?.querySelector('canvas');
          if (canvas) {
            const a  = document.createElement('a');
            a.href   = canvas.toDataURL('image/png');
            a.download = `${btn.dataset.token}.png`;
            a.click();
          } else {
            alert('Please open the NFT panel first so the QR can render, then download.');
          }
        }

        if (action === 'copy') {
          try {
            await navigator.clipboard.writeText(btn.dataset.url);
            const orig = btn.innerHTML;
            btn.textContent = 'Copied!';
            setTimeout(() => { btn.innerHTML = orig; }, 1600);
          } catch {
            prompt('Copy this link:', btn.dataset.url);
          }
        }

        if (action === 'open-redeem') {
          _switchView('redeem');
          el.redeemToken.value = btn.dataset.token;
          _alert(el.redeemAlert, 'info', 'Token loaded. Click "Verify & Redeem" to process it.');
          el.redeemResult.classList.add('hidden');
        }
      });
    });
  }

  /* ================================================================
     CREATE NFT — Generate batch → PublishNft CloudScript → PlayFab
     ================================================================ */
  function _bindFileDrop() {
    if (!el.nftImage) return;

    el.nftImage.addEventListener('change', _onFileChosen);

    el.fileDrop.addEventListener('dragover', e => { e.preventDefault(); el.fileDrop.classList.add('dragover'); });
    el.fileDrop.addEventListener('dragleave', () => el.fileDrop.classList.remove('dragover'));
    el.fileDrop.addEventListener('drop', (e) => {
      e.preventDefault();
      el.fileDrop.classList.remove('dragover');
      if (e.dataTransfer.files.length) {
        el.nftImage.files = e.dataTransfer.files; // modern browsers
        _onFileChosen();
      }
    });
  }

  function _onFileChosen() {
    const file = el.nftImage.files[0];
    if (!file) return;
    if (el.filePreviewName) {
      el.filePreviewName.textContent = file.name;
    }
  }

  function _bindCreate() {
    el.nftForm.addEventListener('submit', async (e) => {
      e.preventDefault();

      const file  = el.nftImage.files[0];
      const name  = el.nftName.value.trim();
      const count = Number(el.nftCount.value);

      if (!name)      return _alert(el.createAlert, 'error', 'Please enter an NFT name.');
      if (!file)      return _alert(el.createAlert, 'error', 'Please upload an NFT image.');
      if (count < 1)  return _alert(el.createAlert, 'error', 'QR quantity must be at least 1.');
      if (count > 50) return _alert(el.createAlert, 'error', 'Maximum 50 QR codes per batch.');

      _setLoading(el.createBtn, true);
      _alert(el.createAlert, 'info', 'Compressing image and building QR batch…');

      try {
        /* 1. Resize + compress image for PlayFab storage limits */
        const image = await KangiService.resizeImage(file, 240);

        /* 2. Build NFT object with unique tokens — mirrors C# approach */
        const nftId  = _uid('COL');
        const base   = location.origin + location.pathname;

        const codes = Array.from({ length: count }, (_, i) => {
          const token = _uid('QR');
          return {
            token,
            serial:     `${nftId}-${String(i + 1).padStart(4, '0')}`,
            redeemed:   false,
            redeemedAt: null,
            url:        `${base}#redeem/${token}`
          };
        });

        const nftObj = {
          id:          nftId,
          name,
          description: el.nftDesc.value.trim(),
          image,
          createdAt:   new Date().toISOString(),
          codes
        };

        /* 3. Save to PlayFab via CloudScript publishNft action */
        _alert(el.createAlert, 'info', 'Saving to database…');
        const res = await KangiService.publishNft(nftObj);

        if (res && res.success !== false) {
          _alert(el.createAlert, 'success',
            `✓ ${count} unique QR code${count > 1 ? 's' : ''} created for "${name}" and saved.`);

          /* Reset form */
          el.nftForm.reset();
          if (el.filePreview) el.filePreview.classList.add('hidden');
          if (el.filePreviewName) el.filePreviewName.textContent = 'No file chosen';

          /* Reload data and switch to manage tab */
          await _loadAllData();
          setTimeout(() => _switchView('manage'), 900);
        } else {
          _alert(el.createAlert, 'error', (res && res.error) || 'Failed to save NFT. Please try again.');
        }

      } catch (err) {
        _alert(el.createAlert, 'error', typeof err === 'string' ? err : err.message || 'An error occurred.');
      } finally {
        _setLoading(el.createBtn, false);
      }
    });
  }

  /* ================================================================
     REDEEM — Verify token via CloudScript redeemToken action
     ================================================================ */
  function _bindRedeem() {
    el.redeemBtn.addEventListener('click', _processRedeem);

    /* Also allow pressing Enter in the token field */
    el.redeemToken.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') _processRedeem();
    });
  }

  async function _processRedeem() {
    const token = el.redeemToken.value.trim();
    if (!token) return _alert(el.redeemAlert, 'error', 'Please paste or enter a QR token.');

    _setLoading(el.redeemBtn, true);
    el.redeemResult.classList.add('hidden');
    _alert(el.redeemAlert, 'info', 'Verifying token with the server…');

    try {
      const res = await KangiService.redeemToken(token);

      if (res && res.success) {
        _alert(el.redeemAlert, 'success', '✓ QR code redeemed successfully. This token has been marked as used.');
        _showRedeemSuccess(res.nft, res.code);
        await _loadAllData(); /* refresh stats */
      } else {
        _alert(el.redeemAlert, 'error', (res && res.error) || 'Redemption failed.');
      }

    } catch (err) {
      _alert(el.redeemAlert, 'error', typeof err === 'string' ? err : err.message || 'Server error during redemption.');
    } finally {
      _setLoading(el.redeemBtn, false);
    }
  }

  function _showRedeemSuccess(nft, code) {
    el.redeemResult.innerHTML = `
      <div class="redeem-result-head">
        <img class="redeem-result-img" src="${nft.image}" alt="${_esc(nft.name)}" />
        <div>
          <strong style="font-size:0.95rem;">${_esc(nft.name)}</strong>
          <div class="nft-meta-row" style="margin-top:.4rem;">
            <span class="chip chip--red">✓ Redeemed</span>
            <span class="chip chip--purple">${_esc(code.serial)}</span>
          </div>
          ${nft.description ? `<div style="font-size:0.75rem;color:var(--text-2);margin-top:.35rem;">${_esc(nft.description)}</div>` : ''}
        </div>
      </div>
      <div class="redeem-result-body">
        <strong>Token:</strong> ${_esc(code.token)}<br/>
        <strong>Redeemed at:</strong> ${new Date(code.redeemedAt).toLocaleString()}
      </div>`;
    el.redeemResult.classList.remove('hidden');
  }

  /* ── Hash routing: #redeem/<token> ── */
  function _checkHashRedeem() {
    const hash = location.hash;
    if (hash.startsWith('#redeem/') && KangiService.session.isLoggedIn()) {
      const token = decodeURIComponent(hash.replace('#redeem/', ''));
      if (token) {
        _switchView('redeem');
        el.redeemToken.value = token;
        _processRedeem();
      }
    }
  }

  window.addEventListener('hashchange', () => {
    if (location.hash.startsWith('#redeem/') && KangiService.session.isLoggedIn()) {
      _checkHashRedeem();
    }
  });

  /* ================================================================
     SOUNDS — Management of Audio Tracks
     ================================================================ */
  function _bindSounds() {
    if (!el.soundsLibrary) return;

    el.btnFilterAllSongs?.addEventListener('click', () => _setSongFilter('all'));
    el.btnFilterPendingSongs?.addEventListener('click', () => _setSongFilter('pending'));
    el.btnFilterApprovedSongs?.addEventListener('click', () => _setSongFilter('approved'));
  }

  function _setSongFilter(filter) {
    state.songFilter = filter;
    document.querySelectorAll('#soundsView .btn').forEach(btn => btn.classList.remove('active'));
    
    if (filter === 'all') el.btnFilterAllSongs?.classList.add('active');
    if (filter === 'pending') el.btnFilterPendingSongs?.classList.add('active');
    if (filter === 'approved') el.btnFilterApprovedSongs?.classList.add('active');

    _renderSongsList();
  }

  async function _loadSongsData() {
    if (!el.soundsLibrary) return;
    el.soundsLibrary.innerHTML = `
      <div class="empty-state">
        <div class="btn-loader"></div>
        <p>Loading songs from the server...</p>
      </div>`;
    try {
      const res = await KangiService.getSongs();
      state.songs = (res && Array.isArray(res.songs)) ? res.songs : 
                    ((res && res.songs) ? res.songs : []);
    } catch (e) {
      console.error('[Kangi] Songs load error:', e);
      state.songs = [];
    }
    _renderSongsList();
  }

  function _renderSongsList() {
    if (!el.soundsLibrary) return;

    let filtered = state.songs;
    if (state.songFilter === 'pending') {
      filtered = state.songs.filter(s => s.isPending === true || s.isPending === "true");
    } else if (state.songFilter === 'approved') {
      filtered = state.songs.filter(s => s.isPending === false || s.isPending === "false" || !s.isPending);
    }

    if (!filtered.length) {
      el.soundsLibrary.innerHTML = `
        <div class="empty-state">
          <svg viewBox="0 0 20 20" fill="currentColor" style="width:40px;height:40px;opacity:0.4;color:var(--pink);"><path fill-rule="evenodd" d="M18 3a1 1 0 00-1.196-.98l-10 2A1 1 0 006 5v9.114A4.369 4.369 0 005 14c-1.657 0-3 .895-3 2s1.343 2 3 2 3-.895 3-2V7.82l8-1.6v5.894A4.37 4.37 0 0015 12c-1.657 0-3 .895-3 2s1.343 2 3 2 3-.895 3-2V3z" clip-rule="evenodd"/></svg>
          <p>No songs found in this category</p>
        </div>`;
      return;
    }

    el.soundsLibrary.innerHTML = '';
    filtered.forEach(song => {
      const isPending = song.isPending === true || song.isPending === "true";
      const songId = song.SongId || song.id;
      const title = song.SongName || song.title || song.SongTitle || song.songTitle || song.Title || song.name || song.Name || "Untitled Song";
      const artist = song.Singer || song.artist || song.artistName || song.SingerName || "Unknown Artist";
      const cover = song.CoverUrl || song.cover || "";
      const songUrl = song.SongUrl || song.songLink || song.url || song.musicUrl || "";

      const row = document.createElement('div');
      row.className = 'song-item';
      row.innerHTML = `
        ${cover ? `
          <img class="song-cover" src="${_esc(cover)}" alt="Cover" data-action="preview" data-url="${_esc(songUrl)}" style="cursor:pointer;" title="Click to play/pause" />
        ` : `
          <div class="song-cover" style="display:grid;place-items:center;background:rgba(236,72,153,0.15);color:var(--pink);cursor:pointer;" data-action="preview" data-url="${_esc(songUrl)}" title="Click to play/pause">
            <svg viewBox="0 0 20 20" fill="currentColor" style="width:20px;height:20px;"><path fill-rule="evenodd" d="M18 3a1 1 0 00-1.196-.98l-10 2A1 1 0 006 5v9.114A4.369 4.369 0 005 14c-1.657 0-3 .895-3 2s1.343 2 3 2 3-.895 3-2V7.82l8-1.6v5.894A4.37 4.37 0 0015 12c-1.657 0-3 .895-3 2s1.343 2 3 2 3-.895 3-2V3z" clip-rule="evenodd"/></svg>
          </div>
        `}
        <div class="song-meta">
          <span class="song-title-text">${_esc(title)}</span>
          <span class="song-artist-text">${_esc(artist)}</span>
          <div style="margin-top:0.35rem;">
            <span class="chip ${isPending ? 'chip--red' : 'chip--green'}">${isPending ? 'Pending' : 'Available'}</span>
          </div>
        </div>
        
        ${songUrl ? `
          <button class="song-preview-btn" data-action="preview" data-url="${_esc(songUrl)}" title="Preview song">
            <svg viewBox="0 0 20 20" fill="currentColor" style="width:16px;height:16px;"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM9.555 7.168A1 1 0 008 8v4a1 1 0 001.555.832l3-2a1 1 0 000-1.664l-3-2z" clip-rule="evenodd"/></svg>
          </button>
        ` : ''}

        <div class="song-actions">
          ${isPending ? `
            <button class="btn btn-primary btn-sm" data-action="approve" data-id="${songId}">Approve</button>
          ` : ''}
          <button class="btn btn-danger btn-sm" data-action="delete" data-id="${songId}">Delete</button>
        </div>
      `;

      el.soundsLibrary.appendChild(row);
    });
  }

  // Handle Approve/Delete/Preview actions
  document.getElementById('soundsView')?.addEventListener('click', async (e) => {
    const btn = e.target.closest('[data-action]');
    if (!btn) return;
    const action = btn.dataset.action;
    const songId = btn.dataset.id;

    if (action === 'preview') {
      const url = btn.dataset.url;
      if (!url) {
        _alert(el.soundsAlert, 'warning', 'No audio URL found for this song.');
        return;
      }
      
      let aud = document.getElementById('song-preview-player');
      if (!aud) {
        aud = document.createElement('audio');
        aud.id = 'song-preview-player';
        document.body.appendChild(aud);
      }
      
      if (aud.dataset.currentUrl === url && !aud.paused) {
        aud.pause();
        /* reset play icon */
        document.querySelectorAll('.song-preview-btn').forEach(b => {
          b.innerHTML = `<svg viewBox="0 0 20 20" fill="currentColor" style="width:16px;height:16px;"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM9.555 7.168A1 1 0 008 8v4a1 1 0 001.555.832l3-2a1 1 0 000-1.664l-3-2z" clip-rule="evenodd"/></svg>`;
        });
      } else {
        aud.src = url;
        aud.dataset.currentUrl = url;
        aud.currentTime = 0;
        aud.play().then(() => {
          document.querySelectorAll('.song-preview-btn').forEach(b => {
            b.innerHTML = `<svg viewBox="0 0 20 20" fill="currentColor" style="width:16px;height:16px;"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM9.555 7.168A1 1 0 008 8v4a1 1 0 001.555.832l3-2a1 1 0 000-1.664l-3-2z" clip-rule="evenodd"/></svg>`;
          });
          btn.innerHTML = `<svg viewBox="0 0 20 20" fill="currentColor" style="width:16px;height:16px;"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zM7 8a1 1 0 012 0v4a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v4a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd"/></svg>`;
        }).catch((err) => {
          console.error('[Kangi Audio Error]', err);
          _alert(el.soundsAlert, 'error', 'Could not play audio. Please verify the Cloudinary link format or check your browser permissions.');
        });
      }
    }

    if (action === 'approve') {
      btn.disabled = true;
      btn.textContent = 'Approving...';
      try {
        const res = await KangiService.approveSong(songId);
        if (res && res.success) {
          _alert(el.soundsAlert, 'success', 'Song approved successfully.');
          await _loadSongsData();
        } else {
          _alert(el.soundsAlert, 'error', res.error || 'Failed to approve song.');
        }
      } catch (err) {
        _alert(el.soundsAlert, 'error', err);
      } finally {
        btn.disabled = false;
        btn.textContent = 'Approve';
      }
    }

    if (action === 'delete') {
      if (!confirm('Are you sure you want to permanently delete this song from the server?')) return;
      btn.disabled = true;
      btn.textContent = 'Deleting...';
      try {
        const res = await KangiService.deleteSong(songId);
        if (res && res.success) {
          _alert(el.soundsAlert, 'success', 'Song deleted successfully.');
          await _loadSongsData();
        } else {
          _alert(el.soundsAlert, 'error', res.error || 'Failed to delete song.');
        }
      } catch (err) {
        _alert(el.soundsAlert, 'error', err);
      } finally {
        btn.disabled = false;
        btn.textContent = 'Delete';
      }
    }
  });

  /* ================================================================
     CLEAR ALL DATA
     ================================================================ */
  function _bindClearAll() {
    el.clearAllBtn.addEventListener('click', async () => {
      if (!confirm('⚠️ This will permanently delete ALL NFT data from the database.\n\nThis cannot be undone. Continue?')) return;

      el.clearAllBtn.disabled    = true;
      el.clearAllBtn.textContent = 'Clearing…';

      try {
        await KangiService.clearAll();
        await _loadAllData();
      } catch (err) {
        alert('Error clearing data: ' + err);
      } finally {
        el.clearAllBtn.disabled    = false;
        el.clearAllBtn.innerHTML   = `
          <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd"/></svg>
          Clear All`;
      }
    });
  }

  /* ================================================================
     REFRESH
     ================================================================ */
  function _bindRefresh() {
    el.refreshBtn.addEventListener('click', async () => {
      if (!KangiService.session.isLoggedIn()) return;
      el.refreshBtn.style.animation = 'spin 0.65s linear';
      await _loadAllData();
      await _loadSongsData();
      setTimeout(() => el.refreshBtn.style.animation = '', 700);
      setTimeout(() => el.refreshBtn.style.animation = '', 700);
    });
  }

  /* ================================================================
     THEME
     ================================================================ */
  function _bindTheme() {
    el.themeToggle.addEventListener('click', () => {
      const cur  = document.documentElement.getAttribute('data-theme') || 'dark';
      const next = cur === 'dark' ? 'light' : 'dark';
      _applyTheme(next);
      localStorage.setItem('kangi_theme', next);
    });
  }

  function _applyTheme(t) {
    document.documentElement.setAttribute('data-theme', t);
    el.iconMoon?.classList.toggle('hidden', t === 'light');
    el.iconSun?.classList.toggle('hidden',  t === 'dark');
  }

  /* ================================================================
     HELPERS
     ================================================================ */
  function _uid(prefix = 'ID') {
    return `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`.toUpperCase();
  }

  /* Stable CSS-safe ID from token */
  function _safeId(token) {
    return token.replace(/[^a-zA-Z0-9]/g, '_');
  }

  function _esc(s = '') {
    return String(s).replace(/[&<>"']/g, c =>
      ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;' }[c])
    );
  }

  function _alert(node, type, text) {
    const cls = { error:'alert--error', success:'alert--success', info:'alert--info', warning:'alert--warning' };
    node.className   = `alert ${cls[type] || 'alert--info'}`;
    node.textContent = text;
    node.classList.remove('hidden');
  }

  function _hideEl(node) { node.classList.add('hidden'); }

  function _setLoading(btn, on) {
    btn.disabled = on;
    btn.querySelector('.btn-text')?.classList.toggle('hidden', on);
    btn.querySelector('.btn-loader')?.classList.toggle('hidden', !on);
  }

  function _emptyHTML(msg) {
    return `<div class="empty-state">${_nftSvg()}<p>${msg}</p></div>`;
  }

  function _nftSvg() {
    return `<svg viewBox="0 0 40 40" fill="none"><rect x="5" y="5" width="30" height="30" rx="6" stroke="currentColor" stroke-width="2"/><path d="M13 27V13h5l7 9V13h5v14h-5L18 18v9h-5Z" fill="currentColor"/></svg>`;
  }

  /* ── Start ── */
  document.addEventListener('DOMContentLoaded', boot);

})();
