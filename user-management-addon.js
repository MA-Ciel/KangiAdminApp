// ===================================================================
// USER MANAGEMENT ENHANCEMENTS - Search, Modals, Ban Duration
// Add these functions to app.js
// ===================================================================

// Add to boot() function:
// _bindUserSearch();
// _bindUserModals();

// User Search Functionality
function _bindUserSearch() {
  if (!el.userSearchInput) return;

  el.userSearchInput.addEventListener('input', (e) => {
    const query = e.target.value.trim().toLowerCase();
    
    if (query.length > 0) {
      el.clearSearchBtn.style.display = 'block';
    } else {
      el.clearSearchBtn.style.display = 'none';
    }

    _filterUsers(query);
  });

  el.clearSearchBtn?.addEventListener('click', () => {
    el.userSearchInput.value = '';
    el.clearSearchBtn.style.display = 'none';
    el.userSearchStats.classList.add('hidden');
    _filterUsers('');
  });
}

function _filterUsers(query) {
  if (!query) {
    state.filteredUsers = state.allUsers;
    _renderUsers(state.allUsers);
    el.userSearchStats.classList.add('hidden');
    return;
  }

  const filtered = state.allUsers.filter(user => {
    const displayName = (user.displayName || '').toLowerCase();
    const email = (user.email || '').toLowerCase();
    const playFabId = (user.playFabId || '').toLowerCase();
    
    return displayName.includes(query) || 
           email.includes(query) || 
           playFabId.includes(query);
  });

  state.filteredUsers = filtered;
  _renderUsers(filtered);

  // Show search stats
  el.userSearchStats.classList.remove('hidden');
  el.userSearchStats.innerHTML = `
    <div class="search-stats-text">
      <svg viewBox="0 0 20 20" fill="currentColor" style="width:16px;height:16px;">
        <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd"/>
      </svg>
      <span>Found <span class="search-stats-count">${filtered.length}</span> user${filtered.length !== 1 ? 's' : ''} matching "${_esc(query)}"</span>
    </div>
  `;
}

// User Details Modal
function _bindUserModals() {
  // Close user details modal
  el.closeUserModal?.addEventListener('click', () => {
    el.userDetailsModal.classList.add('hidden');
  });

  el.closeUserModalBtn?.addEventListener('click', () => {
    el.userDetailsModal.classList.add('hidden');
  });

  el.userDetailsModal?.querySelector('.modal-overlay')?.addEventListener('click', () => {
    el.userDetailsModal.classList.add('hidden');
  });

  // Close ban duration modal
  el.closeBanModal?.addEventListener('click', () => {
    el.banDurationModal.classList.add('hidden');
  });

  el.cancelBanBtn?.addEventListener('click', () => {
    el.banDurationModal.classList.add('hidden');
  });

  el.banDurationModal?.querySelector('.modal-overlay')?.addEventListener('click', () => {
    el.banDurationModal.classList.add('hidden');
  });

  // Ban duration button clicks
  el.banDurationModal?.addEventListener('click', async (e) => {
    const btn = e.target.closest('.ban-option-btn');
    if (!btn || !state.banTarget) return;

    const duration = btn.dataset.duration;
    el.banDurationModal.classList.add('hidden');

    // Perform ban with duration
    await _banUser WithDuration(state.banTarget, duration);
    state.banTarget = null;
  });
}

function _showUserDetails(user) {
  if (!el.userDetailsModal) return;

  el.userDetailsModal.classList.remove('hidden');
  el.userDetailsBody.innerHTML = `
    <div class="loading-spinner">
      <div class="btn-loader"></div>
      <p>Loading user details...</p>
    </div>
  `;

  // Build user details
  const initial = (user.displayName || '?').charAt(0).toUpperCase();
  const unlockedChars = user.unlockedCharacters || [];
  const allCharacters = ['Katsumi', 'Kiko', 'Bee', 'Chyna'];

  setTimeout(() => {
    el.userDetailsBody.innerHTML = `
      <div class="user-detail-header">
        <div class="user-detail-avatar">
          ${user.avatarUrl 
            ? `<img src="${_esc(user.avatarUrl)}" alt="${_esc(user.displayName)}" />` 
            : initial
          }
        </div>
        <div class="user-detail-info">
          <h4>${_esc(user.displayName || 'Unknown')}</h4>
          <span class="user-detail-email">${_esc(user.email || 'No email')}</span>
          <div class="user-detail-badges">
            ${user.isAdmin ? '<span class="chip chip--purple">Admin</span>' : ''}
            ${user.isBanned ? '<span class="chip chip--red">Banned</span>' : '<span class="chip chip--green">Active</span>'}
          </div>
        </div>
      </div>

      <div class="detail-section">
        <h5>Account Information</h5>
        <div class="detail-grid">
          <div class="detail-item">
            <span class="detail-item-label">PlayFab ID</span>
            <span class="detail-item-value">${_esc(user.playFabId)}</span>
          </div>
          <div class="detail-item">
            <span class="detail-item-label">Display Name</span>
            <span class="detail-item-value">${_esc(user.displayName || 'Not set')}</span>
          </div>
          <div class="detail-item">
            <span class="detail-item-label">Email</span>
            <span class="detail-item-value">${_esc(user.email || 'Not set')}</span>
          </div>
          <div class="detail-item">
            <span class="detail-item-label">Created</span>
            <span class="detail-item-value">${user.created ? new Date(user.created).toLocaleDateString() : '—'}</span>
          </div>
          <div class="detail-item">
            <span class="detail-item-label">Last Login</span>
            <span class="detail-item-value">${user.lastLogin ? new Date(user.lastLogin).toLocaleDateString() : 'Never'}</span>
          </div>
          <div class="detail-item">
            <span class="detail-item-label">Status</span>
            <span class="detail-item-value">${user.isBanned ? '🔴 Banned' : '🟢 Active'}</span>
          </div>
        </div>
      </div>

      <div class="detail-section">
        <h5>Unlocked Characters (${unlockedChars.length}/${allCharacters.length})</h5>
        <div class="characters-list">
          ${allCharacters.map(char => {
            const isUnlocked = unlockedChars.includes(char);
            return `
              <div class="character-card ${isUnlocked ? 'unlocked' : 'locked'}">
                <div class="character-icon">${isUnlocked ? '🔓' : '🔒'}</div>
                <span class="character-name">${_esc(char)}</span>
                <span class="character-status ${isUnlocked ? 'unlocked' : 'locked'}">${isUnlocked ? 'Unlocked' : 'Locked'}</span>
              </div>
            `;
          }).join('')}
        </div>
      </div>
    `;
  }, 300);
}

function _showBanModal(userName) {
  if (!el.banDurationModal) return;

  el.banUserName.textContent = `Select ban duration for "${userName}"`;
  el.banDurationModal.classList.remove('hidden');
}

async function _banUserWithDuration(target, duration) {
  const { email, pfid, name } = target;

  if (!el.usersAlert) return;

  _alert(el.usersAlert, 'info', `Banning ${name}...`);

  try {
    let durationDays = duration === 'permanent' ? 0 : parseInt(duration);
    const res = await KangiService.banUser(email, pfid, durationDays);

    if (res && res.success) {
      _alert(el.usersAlert, 'success', `${name} has been banned ${duration === 'permanent' ? 'permanently' : `for ${duration} days`}.`);
      await _loadUsers(); // Refresh list
    } else {
      _alert(el.usersAlert, 'error', (res && res.error) || 'Ban failed.');
    }
  } catch (err) {
    _alert(el.usersAlert, 'error', `Ban failed: ${String(err)}`);
  }
}
