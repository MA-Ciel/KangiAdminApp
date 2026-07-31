/* ============================================================
   service.js — Data & Auth Service Layer
   Mirrors the C# LoginManager + AdminManager flow in JS.
   Title ID: 1D5959 (hardcoded, never exposed in UI)
   ============================================================ */

const KangiService = (function () {

  /* ── Config ── */
  const TITLE_ID     = '1D5959';
  const NFT_DATA_KEY = 'GlobalAppNftsMasterList';

  /* ── Session State ── */
  const session = {
    playFabId:   null,
    email:       null,
    displayName: null,
    isAdmin:     false,
    isLoggedIn:  () => !!session.playFabId
  };

  /* ── Boot: set title ── */
  function init() {
    PlayFab.settings.titleId = TITLE_ID;
  }

  /* ============================================================
     AUTH — Login with Email + Password   (mirrors LoginManager.cs)
     1. LoginWithEmailAddress
     2. GetUserData → check IsAdmin key
     3. Resolve with { success, isAdmin, displayName } or reject
     ============================================================ */
  function login(email, password) {
    return new Promise((resolve, reject) => {
      PlayFabClientSDK.LoginWithEmailAddress(
        {
          TitleId:  TITLE_ID,
          Email:    email.trim(),
          Password: password,
          InfoRequestParameters: { GetPlayerProfile: true }
        },
        (result, error) => {
          if (error) {
            reject(_friendlyError(error));
            return;
          }
          /* Store session basics */
          session.playFabId   = result.data.PlayFabId;
          session.email       = email.trim();
          session.displayName = result.data.InfoResultPayload?.PlayerProfile?.DisplayName || '';

          /* Admin check — mirrors AdminManager.CheckAdminStatus() */
          _checkAdminStatus()
            .then(isAdmin => {
              if (!isAdmin) {
                /* Not an admin — clear session, deny access */
                _clearSession();
                reject('Access denied. This account does not have administrator privileges.');
              } else {
                resolve({
                  success:     true,
                  isAdmin:     true,
                  playFabId:   session.playFabId,
                  displayName: session.displayName,
                  email:       session.email
                });
              }
            })
            .catch(() => {
              _clearSession();
              reject('Could not verify account permissions. Please try again.');
            });
        }
      );
    });
  }

  /* ── Admin Status Check  (mirrors AdminManager.cs → GetUserData IsAdmin) ── */
  function _checkAdminStatus() {
    return new Promise((resolve) => {
      PlayFabClientSDK.GetUserData(
        { Keys: ['IsAdmin'] },
        (result, error) => {
          if (error || !result?.data?.Data) {
            session.isAdmin = false;
            resolve(false);
            return;
          }
          const val = result.data.Data?.IsAdmin?.Value ?? '';
          const isAdmin = val.toLowerCase() === 'true';
          session.isAdmin = isAdmin;
          resolve(isAdmin);
        }
      );
    });
  }

  /* ── Logout ── */
  function logout() {
    PlayFabClientSDK.ForgetAllCredentials();
    _clearSession();
  }

  function _clearSession() {
    session.playFabId   = null;
    session.email       = null;
    session.displayName = null;
    session.isAdmin     = false;
  }

  /* ============================================================
     NFT DATA  (mirrors nftQrWorkflow CloudScript handlers)
     ============================================================ */

  /* Call the CloudScript nftQrWorkflow handler */
  function _callNftScript(action, args) {
    return new Promise((resolve, reject) => {
      PlayFabClientSDK.ExecuteCloudScript(
        {
          FunctionName:       'nftQrWorkflow',
          FunctionParameter:  { action, ...args },
          GeneratePlayStreamEvent: true
        },
        (result, error) => {
          if (error) { reject(_friendlyError(error)); return; }
          const fn = result?.data?.FunctionResult;
          if (!fn) { reject('No response from server function.'); return; }
          if (fn.error) { reject(fn.error); return; }
          resolve(fn);
        }
      );
    });
  }

  /* Fetch all NFTs */
  function getNfts() { return _callNftScript('getNfts', {}); }

  /* Publish a new NFT batch */
  function publishNft(nftData) { return _callNftScript('publishNft', { nftData }); }

  /* Redeem a token */
  function redeemToken(token) { return _callNftScript('redeemToken', { token }); }

  /* Wipe all NFT data */
  function clearAll() { return _callNftScript('clearAll', {}); }

  /* ============================================================
     SONGS & MUSIC DATA  (calls videoAppWorkflow)
     ============================================================ */

  /* Call the CloudScript videoAppWorkflow handler */
  function _callVideoScript(action, args) {
    return new Promise((resolve, reject) => {
      PlayFabClientSDK.ExecuteCloudScript(
        {
          FunctionName:       'videoAppWorkflow',
          FunctionParameter:  { action, ...args },
          GeneratePlayStreamEvent: true
        },
        (result, error) => {
          if (error) { reject(_friendlyError(error)); return; }
          const fn = result?.data?.FunctionResult;
          if (!fn) { reject('No response from server function.'); return; }
          let parsed = fn;
          if (typeof fn === 'string') {
            try {
              parsed = JSON.parse(fn);
            } catch (e) {
              reject('Invalid response format.');
              return;
            }
          }
          if (parsed.error) { reject(parsed.error); return; }
          resolve(parsed);
        }
      );
    });
  }

  /* Fetch all pending, approved, and total songs */
  function getSongs() { return _callVideoScript('getSongs', {}); }

  /* Approve a pending song */
  function approveSong(songId) { return _callVideoScript('approveSong', { adminData: { songId } }); }

  /* Delete a song from server catalog */
  function deleteSong(songId) { return _callVideoScript('deleteSong', { adminData: { songId } }); }

  /* ============================================================
     USER MANAGEMENT  (calls adminUserWorkflow)
     ============================================================ */

  /* Internal helper for adminUserWorkflow CloudScript */
  function _callAdminScript(action, args) {
    return new Promise((resolve, reject) => {
      PlayFabClientSDK.ExecuteCloudScript(
        {
          FunctionName:            'adminUserWorkflow',
          FunctionParameter:       { action, ...args },
          GeneratePlayStreamEvent: true
        },
        (result, error) => {
          if (error) { reject(_friendlyError(error)); return; }
          const fn = result?.data?.FunctionResult;
          if (!fn) { reject('No response from server function.'); return; }
          if (fn.error) { reject(fn.error); return; }
          resolve(fn);
        }
      );
    });
  }

  /* Grant admin role to a user by email */
  function makeAdmin(email) { return _callAdminScript('makeAdmin', { email }); }

  /* Revoke admin role from a user by email */
  function revokeAdmin(email) { return _callAdminScript('revokeAdmin', { email }); }

  /* Fetch all registered users */
  function getAllUsers() { return _callAdminScript('getAllUsers', {}); }

  /* Ban a user by email */
  function banUser(email) { return _callAdminScript('banUser', { email }); }

  /* Unban a user by email */
  function unbanUser(email) { return _callAdminScript('unbanUser', { email }); }

  /* ============================================================
     IMAGE UTIL — resize + compress before storing
     ============================================================ */
  function resizeImage(file, maxWidth = 240) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onerror = reject;
      reader.onload = (e) => {
        const img = new Image();
        img.onerror = reject;
        img.onload = () => {
          const canvas = document.createElement('canvas');
          let w = img.width, h = img.height;
          if (w > maxWidth) { h = Math.round(h * maxWidth / w); w = maxWidth; }
          canvas.width = w; canvas.height = h;
          canvas.getContext('2d').drawImage(img, 0, 0, w, h);
          resolve(canvas.toDataURL('image/jpeg', 0.55));
        };
        img.src = e.target.result;
      };
      reader.readAsDataURL(file);
    });
  }

  /* ── Error Helper (mirrors GetFriendlyError in C#) ── */
  function _friendlyError(err) {
    const code = err?.errorCode ?? err?.error ?? '';
    const msg  = err?.errorMessage ?? err?.message ?? 'An unexpected error occurred.';
    const map  = {
      1001: 'Invalid email address.',
      1002: 'Incorrect password. Please try again.',
      1009: 'No account found with this email address.',
      1246: 'This email is already registered.',
      1212: 'Incorrect email or password.'
    };
    return map[code] || msg;
  }

  /* ── Public API ── */
  return {
    init,
    login,
    logout,
    session,
    getNfts,
    publishNft,
    redeemToken,
    clearAll,
    resizeImage,
    getSongs,
    approveSong,
    deleteSong,
    makeAdmin,
    revokeAdmin,
    getAllUsers,
    banUser,
    unbanUser
  };

})();
