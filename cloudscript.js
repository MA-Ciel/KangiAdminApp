// ====================================================================================
// MASTER WORKFLOW FOR VIDEO & MUSIC APPLICATION
// Fully Optimized to Prevent Crashes and Ensure Deep Server Wipes
// Copy and Paste this entire file into your PlayFab CloudScript Revision Editor.
// ====================================================================================

handlers.videoAppWorkflow = function (args, context) {
    var action = args.action;

    // --- 1. GLOBAL DATABASE KEYS DEFINITIONS ---
    var VIDEOS_DATABASE_KEY = "GlobalAppVideosMasterList";
    var SONGS_DATABASE_KEY = "GlobalAppSongsMasterList";

    // ====================================================================================
    // USER ACTIONS (CLIENT PIPELINE)
    // ====================================================================================

    // A. FETCH VIDEO FEED DATA
    if (action === "getFeed") {
        var serverData = server.GetTitleInternalData({ Keys: [VIDEOS_DATABASE_KEY] });
        if (serverData.Data && serverData.Data[VIDEOS_DATABASE_KEY]) {
            return serverData.Data[VIDEOS_DATABASE_KEY];
        }
        return JSON.stringify({ videos: [] });
    }

    // B. PUBLISH NEW UPLOADED VIDEO
    if (action === "publish") {
        var newVideoObj = args.videoData;

        newVideoObj.likes = 0;
        newVideoObj.commentsCount = 0;
        newVideoObj.shares = 0;

        var currentFeedJson = server.GetTitleInternalData({ Keys: [VIDEOS_DATABASE_KEY] });
        var masterVideosList = [];

        if (currentFeedJson.Data && currentFeedJson.Data[VIDEOS_DATABASE_KEY]) {
            var parsedData = JSON.parse(currentFeedJson.Data[VIDEOS_DATABASE_KEY]);
            masterVideosList = parsedData.videos || [];
        }

        masterVideosList.unshift(newVideoObj);

        server.SetTitleInternalData({
            Key: VIDEOS_DATABASE_KEY,
            Value: JSON.stringify({ videos: masterVideosList })
        });

        return { success: true, message: "Video metadata successfully appended to global stream database." };
    }

    // C. SUBMIT NEW UPLOADED SONG (Global Master Catalog)
    if (action === "submitSong" || action === "submit") {
        var newSongObj = args.songData;
        newSongObj.isPending = true;

        var currentSongsJson = server.GetTitleInternalData({ Keys: [SONGS_DATABASE_KEY] });
        var songsList = [];
        var approvedList = [];
        var pendingList = [];

        if (currentSongsJson.Data && currentSongsJson.Data[SONGS_DATABASE_KEY]) {
            try {
                var parsedSongsData = JSON.parse(currentSongsJson.Data[SONGS_DATABASE_KEY]);
                if (Array.isArray(parsedSongsData)) {
                    songsList = parsedSongsData;
                } else if (parsedSongsData.songs) {
                    songsList = parsedSongsData.songs;
                } else {
                    songsList = [];
                    if (parsedSongsData.approvedSongs) songsList = songsList.concat(parsedSongsData.approvedSongs);
                    if (parsedSongsData.pendingSongs) songsList = songsList.concat(parsedSongsData.pendingSongs);
                }
            } catch (e) {
                songsList = [];
            }
        }

        // Add new song to top of master songs list
        songsList.unshift(newSongObj);

        // Separate approved and pending lists
        for (var i = 0; i < songsList.length; i++) {
            if (songsList[i].isPending === true || songsList[i].isPending === "true") {
                pendingList.push(songsList[i]);
            } else {
                approvedList.push(songsList[i]);
            }
        }

        var songsPayload = {
            songs: songsList,
            approvedSongs: approvedList,
            pendingSongs: pendingList
        };

        server.SetTitleInternalData({
            Key: SONGS_DATABASE_KEY,
            Value: JSON.stringify(songsPayload)
        });

        return { success: true, message: "Song metadata successfully submitted to global master catalog." };
    }

    // D. FETCH ALL SONGS (APPROVED & PENDING)
    if (action === "getSongs" || action === "getAll") {
        var musicServerData = server.GetTitleInternalData({ Keys: [SONGS_DATABASE_KEY] });
        if (musicServerData.Data && musicServerData.Data[SONGS_DATABASE_KEY]) {
            try {
                var parsedSongs = JSON.parse(musicServerData.Data[SONGS_DATABASE_KEY]);
                var songsArray = [];
                if (Array.isArray(parsedSongs)) {
                    songsArray = parsedSongs;
                } else if (parsedSongs.songs) {
                    songsArray = parsedSongs.songs;
                } else {
                    return JSON.stringify(parsedSongs);
                }

                var appList = [];
                var pendList = [];
                for (var k = 0; k < songsArray.length; k++) {
                    if (songsArray[k].isPending === true || songsArray[k].isPending === "true") {
                        pendList.push(songsArray[k]);
                    } else {
                        appList.push(songsArray[k]);
                    }
                }

                return JSON.stringify({
                    songs: songsArray,
                    approvedSongs: appList,
                    pendingSongs: pendList
                });
            } catch (e) {
                return JSON.stringify({ songs: [], approvedSongs: [], pendingSongs: [] });
            }
        }
        return JSON.stringify({ songs: [], approvedSongs: [], pendingSongs: [] });
    }

    // E. INTERACTIONS ENGINE (LIKE, UNLIKE, SHARE, COMMENT)
    if (action === "interact") {
        var videoOwnerId = args.ownerId;
        var targetVideoId = args.videoId;
        var interactionType = args.interactionType;

        var dataResponse = server.GetTitleInternalData({ Keys: [VIDEOS_DATABASE_KEY] });
        if (!dataResponse.Data || !dataResponse.Data[VIDEOS_DATABASE_KEY]) {
            return { success: false, error: "Database not initialized" };
        }

        var wrapper = JSON.parse(dataResponse.Data[VIDEOS_DATABASE_KEY]);
        var videos = wrapper.videos || [];
        var targetFound = false;
        var updatedVideo = null;

        for (var i = 0; i < videos.length; i++) {
            if (videos[i].videoId === targetVideoId) {
                targetFound = true;

                if (interactionType === "like") {
                    videos[i].likes += 1;
                    try {
                        server.UpdatePlayerStatistics({
                            PlayFabId: videoOwnerId,
                            Statistics: [{ StatisticName: "TotalLikesReceived", Value: 1 }]
                        });
                    } catch (e) { log.error("Profile statistics update failed: " + e); }
                }
                else if (interactionType === "unlike") {
                    if (videos[i].likes > 0) videos[i].likes -= 1;
                }
                else if (interactionType === "share") {
                    videos[i].shares += 1;
                    try {
                        server.UpdatePlayerStatistics({
                            PlayFabId: videoOwnerId,
                            Statistics: [{ StatisticName: "TotalSharesReceived", Value: 1 }]
                        });
                    } catch (e) { }
                }
                else if (interactionType === "comment") {
                    var commenterUsername = args.username || "Anonymous";
                    var commentText = args.commentText || "";
                    var timestamp = new Date().toISOString();

                    if (!videos[i].comments) {
                        videos[i].comments = [];
                    }
                    videos[i].comments.push({
                        username: commenterUsername,
                        commentText: commentText,
                        timestamp: timestamp
                    });
                    videos[i].commentsCount = videos[i].comments.length;
                }

                updatedVideo = videos[i];
                break;
            }
        }

        if (targetFound) {
            server.SetTitleInternalData({
                Key: VIDEOS_DATABASE_KEY,
                Value: JSON.stringify({ videos: videos })
            });
            return { success: true, type: interactionType, updatedVideo: updatedVideo };
        }

        return { success: false, error: "Video ID lookup key signature matched nothing." };
    }


    // ====================================================================================
    // ADMIN COMMANDS ACTIONS (EDITOR DASHBOARD CONTROL PIPELINE)
    // ====================================================================================
    var adminParams = args.adminData || {};

    // F. APPROVE A PENDING SONG
    if (action === "approveSong") {
        var targetSongId = adminParams.songId;
        var musicDataResponse = server.GetTitleInternalData({ Keys: [SONGS_DATABASE_KEY] });

        if (!musicDataResponse.Data || !musicDataResponse.Data[SONGS_DATABASE_KEY]) {
            return { success: false, error: "Songs catalog empty" };
        }

        var musicWrapper = JSON.parse(musicDataResponse.Data[SONGS_DATABASE_KEY]);
        var songs = [];
        if (Array.isArray(musicWrapper)) {
            songs = musicWrapper;
        } else if (musicWrapper.songs) {
            songs = musicWrapper.songs;
        } else {
            songs = [];
            if (musicWrapper.approvedSongs) songs = songs.concat(musicWrapper.approvedSongs);
            if (musicWrapper.pendingSongs) songs = songs.concat(musicWrapper.pendingSongs);
        }

        var statusUpdated = false;
        var approvedList = [];
        var pendingList = [];

        for (var j = 0; j < songs.length; j++) {
            if (songs[j].SongId === targetSongId) {
                songs[j].isPending = false;
                statusUpdated = true;
            }

            if (songs[j].isPending === true || songs[j].isPending === "true") {
                pendingList.push(songs[j]);
            } else {
                approvedList.push(songs[j]);
            }
        }

        if (statusUpdated) {
            var updatedMusicPayload = {
                songs: songs,
                approvedSongs: approvedList,
                pendingSongs: pendingList
            };
            server.SetTitleInternalData({
                Key: SONGS_DATABASE_KEY,
                Value: JSON.stringify(updatedMusicPayload)
            });
            return { success: true, message: "Song approved successfully." };
        }
        return { success: false, message: "Song ID not found." };
    }

    // G. DELETE A SONG FROM THE PLATFORM
    if (action === "deleteSong") {
        var songIdToDelete = adminParams.songId;
        var resSongs = server.GetTitleInternalData({ Keys: [SONGS_DATABASE_KEY] });

        if (resSongs.Data && resSongs.Data[SONGS_DATABASE_KEY]) {
            var wrapSongs = JSON.parse(resSongs.Data[SONGS_DATABASE_KEY]);
            var allSongsList = [];
            if (Array.isArray(wrapSongs)) {
                allSongsList = wrapSongs;
            } else if (wrapSongs.songs) {
                allSongsList = wrapSongs.songs;
            } else {
                allSongsList = [];
                if (wrapSongs.approvedSongs) allSongsList = allSongsList.concat(wrapSongs.approvedSongs);
                if (wrapSongs.pendingSongs) allSongsList = allSongsList.concat(wrapSongs.pendingSongs);
            }

            allSongsList = allSongsList.filter(function (item) { return item.SongId !== songIdToDelete; });

            var appSongs = [];
            var pendSongs = [];
            for (var m = 0; m < allSongsList.length; m++) {
                if (allSongsList[m].isPending === true || allSongsList[m].isPending === "true") {
                    pendSongs.push(allSongsList[m]);
                } else {
                    appSongs.push(allSongsList[m]);
                }
            }

            var updatedWrap = {
                songs: allSongsList,
                approvedSongs: appSongs,
                pendingSongs: pendSongs
            };

            server.SetTitleInternalData({
                Key: SONGS_DATABASE_KEY,
                Value: JSON.stringify(updatedWrap)
            });
            return { success: true, message: "Song removed from active tables." };
        }
        return { success: false, message: "No catalog active data found." };
    }

    // H. DELETE A VIDEO FEED POST
    if (action === "deleteVideo") {
        var videoIdToDelete = adminParams.videoId;
        var resVideos = server.GetTitleInternalData({ Keys: [VIDEOS_DATABASE_KEY] });

        if (resVideos.Data && resVideos.Data[VIDEOS_DATABASE_KEY]) {
            var wrapVideos = JSON.parse(resVideos.Data[VIDEOS_DATABASE_KEY]);
            var activeVideosList = wrapVideos.videos || [];

            var filteredVideos = activeVideosList.filter(function (v) {
                return v.videoId !== videoIdToDelete;
            });

            server.SetTitleInternalData({
                Key: VIDEOS_DATABASE_KEY,
                Value: JSON.stringify({ videos: filteredVideos })
            });
            return { success: true, message: "Video feed cleaned successfully." };
        }
        return { success: false, message: "Target database feed missing." };
    }

    // I. RESET SERVER COMPLETELY (THE CORE MASTER NUCLEAR WIPE)
    if (action === "resetServer") {
        var doubleCheckConfirmation = adminParams.confirmWipe;

        if (doubleCheckConfirmation === true) {
            // 1. Wipe out videos array structure entirely
            server.SetTitleInternalData({
                Key: VIDEOS_DATABASE_KEY,
                Value: JSON.stringify({ videos: [] })
            });

            // 2. Wipe out songs data fields
            var structuralResetPayload = {
                songs: [],
                approvedSongs: [],
                pendingSongs: []
            };

            server.SetTitleInternalData({
                Key: SONGS_DATABASE_KEY,
                Value: JSON.stringify(structuralResetPayload)
            });

            log.info("Reset Master Workflow: Database clean flush pipeline processed successfully.");
            return { success: true, message: "WIPE COMPLETE" };
        }
        return { success: false, message: "BAD AUTH TOKEN OR SECURITY MISMATCH" };
    }

    return { error: "No matching executable protocol action found inside data layers." };
};


// ====================================================================================
// MASTER WORKFLOW FOR NFT QR GENERATOR
// Copy and Paste this entire file into your PlayFab CloudScript Revision Editor.
// ====================================================================================

handlers.nftQrWorkflow = function (args, context) {
    var action = args.action;

    // --- 1. GLOBAL DATABASE KEY DEFINITION ---
    // We use Title Internal Data to store the master list of NFTs.
    var NFT_DATABASE_KEY = "GlobalAppNftsMasterList";

    // ====================================================================================
    // A. FETCH ALL NFTs
    // ====================================================================================
    if (action === "getNfts") {
        var serverData = server.GetTitleInternalData({ Keys: [NFT_DATABASE_KEY] });
        if (serverData.Data && serverData.Data[NFT_DATABASE_KEY]) {
            try {
                return JSON.parse(serverData.Data[NFT_DATABASE_KEY]);
            } catch (e) {
                return { nfts: [] };
            }
        }
        return { nfts: [] };
    }

    // ====================================================================================
    // B. PUBLISH NEW NFT BATCH
    // ====================================================================================
    if (action === "publishNft") {
        var newNftObj = args.nftData;

        var currentFeedJson = server.GetTitleInternalData({ Keys: [NFT_DATABASE_KEY] });
        var nftsList = [];

        if (currentFeedJson.Data && currentFeedJson.Data[NFT_DATABASE_KEY]) {
            try {
                var parsedData = JSON.parse(currentFeedJson.Data[NFT_DATABASE_KEY]);
                nftsList = parsedData.nfts || [];
            } catch (e) {
                nftsList = [];
            }
        }

        // Add to the beginning of the list
        nftsList.unshift(newNftObj);

        server.SetTitleInternalData({
            Key: NFT_DATABASE_KEY,
            Value: JSON.stringify({ nfts: nftsList })
        });

        return { success: true, message: "NFT batch successfully published to global database." };
    }

    // ====================================================================================
    // C. REDEEM A QR TOKEN
    // ====================================================================================
    if (action === "redeemToken") {
        var targetToken = args.token;

        var dataResponse = server.GetTitleInternalData({ Keys: [NFT_DATABASE_KEY] });
        if (!dataResponse.Data || !dataResponse.Data[NFT_DATABASE_KEY]) {
            return { success: false, error: "Database not initialized. No NFTs exist." };
        }

        var wrapper;
        try {
            wrapper = JSON.parse(dataResponse.Data[NFT_DATABASE_KEY]);
        } catch (e) {
            return { success: false, error: "Database parse error." };
        }

        var nfts = wrapper.nfts || [];
        var targetFound = false;
        var alreadyRedeemed = false;
        var resultNft = null;
        var resultCode = null;

        // Search for the specific token inside all NFTs and their generated codes
        for (var i = 0; i < nfts.length; i++) {
            var nft = nfts[i];
            for (var j = 0; j < nft.codes.length; j++) {
                if (nft.codes[j].token === targetToken) {
                    targetFound = true;
                    if (nft.codes[j].redeemed) {
                        alreadyRedeemed = true;
                    } else {
                        // Mark as redeemed
                        nft.codes[j].redeemed = true;
                        nft.codes[j].redeemedAt = new Date().toISOString();

                        // We return a simplified NFT without all other codes for efficiency
                        resultNft = {
                            id: nft.id,
                            name: nft.name,
                            description: nft.description,
                            image: nft.image,
                            createdAt: nft.createdAt
                        };
                        resultCode = nft.codes[j];
                    }
                    break;
                }
            }
            if (targetFound) break;
        }

        if (targetFound) {
            if (alreadyRedeemed) {
                return { success: false, error: "This QR code has already been redeemed." };
            } else {
                // Save updated state to PlayFab Title Internal Data
                server.SetTitleInternalData({
                    Key: NFT_DATABASE_KEY,
                    Value: JSON.stringify({ nfts: nfts })
                });
                return { success: true, message: "Token redeemed successfully.", nft: resultNft, code: resultCode };
            }
        }

        return { success: false, error: "Token not found. Invalid QR code." };
    }

    // ====================================================================================
    // D. CLEAR ALL DATA (Nuclear Wipe)
    // ====================================================================================
    if (action === "clearAll") {
        server.SetTitleInternalData({
            Key: NFT_DATABASE_KEY,
            Value: JSON.stringify({ nfts: [] })
        });
        return { success: true, message: "All NFT data cleared completely." };
    }

    return { error: "No matching action found in PlayFab cloud script." };
};


// ====================================================================================
// ADMIN USER MANAGEMENT WORKFLOW
// Handles granting admin privileges to a user by email address.
// Copy and Paste this entire file into your PlayFab CloudScript Revision Editor.
// ====================================================================================

handlers.adminUserWorkflow = function (args, context) {
    var action = args.action;

    // ── Shared registry key ──
    var USERS_REGISTRY_KEY = "GlobalAppUsersRegistry";

    // ── Helper: read registry ──
    function _readRegistry() {
        var raw = server.GetTitleInternalData({ Keys: [USERS_REGISTRY_KEY] });
        if (raw.Data && raw.Data[USERS_REGISTRY_KEY]) {
            try { return JSON.parse(raw.Data[USERS_REGISTRY_KEY]); } catch (e) {}
        }
        return [];
    }

    // ── Helper: save registry ──
    function _saveRegistry(list) {
        server.SetTitleInternalData({
            Key:   USERS_REGISTRY_KEY,
            Value: JSON.stringify(list)
        });
    }

    // ── Helper: upsert one user entry into registry ──
    function _upsertRegistry(entry) {
        var list  = _readRegistry();
        var found = false;
        for (var i = 0; i < list.length; i++) {
            if (list[i].playFabId === entry.playFabId) {
                // merge — keep existing fields, overwrite supplied ones
                for (var k in entry) { list[i][k] = entry[k]; }
                found = true;
                break;
            }
        }
        if (!found) list.unshift(entry);
        _saveRegistry(list);
    }

    // ====================================================================================
    // A. REGISTER USER — Called on every login to keep the registry up-to-date
    // ====================================================================================
    if (action === "registerUser") {
        var callerPlayFabId  = currentPlayerId;           // built-in CloudScript var
        var callerEmail      = args.email      || "";
        var callerName       = args.displayName || "";
        var callerAvatar     = args.avatarUrl  || "";

        // Read IsAdmin / IsBanned from the caller's own player data
        var selfData = {};
        try {
            var ud = server.GetUserData({ PlayFabId: callerPlayFabId, Keys: ["IsAdmin", "IsBanned"] });
            selfData = ud.Data || {};
        } catch (e) {}

        var entry = {
            playFabId:   callerPlayFabId,
            displayName: callerName,
            email:       callerEmail,
            avatarUrl:   callerAvatar,
            isAdmin:     (selfData.IsAdmin  && selfData.IsAdmin.Value  === "true"),
            isBanned:    (selfData.IsBanned && selfData.IsBanned.Value === "true"),
            lastLogin:   new Date().toISOString()
        };

        _upsertRegistry(entry);
        return { success: true, message: "User registered in registry." };
    }

    // ====================================================================================
    // B. GET ALL USERS — Return the full registry list
    // ====================================================================================
    if (action === "getAllUsers") {
        var users = _readRegistry();
        return { success: true, users: users, total: users.length };
    }

    // ====================================================================================
    // C. MAKE ADMIN — accepts email OR playFabId, sets IsAdmin = "true"
    // ====================================================================================
    if (action === "makeAdmin") {
        var targetEmail  = args.email      || "";
        var targetPfId   = args.playFabId  || "";

        // Resolve PlayFabId — prefer direct id, fallback to email lookup
        var resolvedId   = "";
        var resolvedName = "";
        var resolvedEmail = targetEmail;

        if (targetPfId) {
            resolvedId = targetPfId;
            try {
                var pfInfo = server.GetUserAccountInfo({ PlayFabId: targetPfId });
                resolvedName  = (pfInfo.UserInfo && pfInfo.UserInfo.TitleInfo && pfInfo.UserInfo.TitleInfo.DisplayName) ? pfInfo.UserInfo.TitleInfo.DisplayName : targetPfId;
                resolvedEmail = (pfInfo.UserInfo && pfInfo.UserInfo.PrivateInfo && pfInfo.UserInfo.PrivateInfo.Email) ? pfInfo.UserInfo.PrivateInfo.Email : targetEmail;
            } catch (e) { resolvedName = targetPfId; }
        } else if (targetEmail) {
            try {
                var emailLookup = server.GetUserAccountInfo({ Email: targetEmail });
                if (!emailLookup || !emailLookup.UserInfo) return { success: false, error: "No account found with that email." };
                resolvedId    = emailLookup.UserInfo.PlayFabId;
                resolvedName  = (emailLookup.UserInfo.TitleInfo && emailLookup.UserInfo.TitleInfo.DisplayName) ? emailLookup.UserInfo.TitleInfo.DisplayName : targetEmail;
            } catch (e) { return { success: false, error: "No account found with that email address." }; }
        } else {
            return { success: false, error: "Email or PlayFabId is required." };
        }

        if (!resolvedId) return { success: false, error: "Could not resolve user." };

        // Set IsAdmin in player data
        server.UpdateUserData({
            PlayFabId:  resolvedId,
            Data:       { "IsAdmin": "true" },
            Permission: "Public"
        });

        // Sync registry
        _upsertRegistry({ playFabId: resolvedId, displayName: resolvedName, email: resolvedEmail, isAdmin: true, isBanned: false });

        return { success: true, message: "Admin granted.", playFabId: resolvedId, displayName: resolvedName, email: resolvedEmail };
    }

    // ====================================================================================
    // D. REVOKE ADMIN — accepts email OR playFabId
    // ====================================================================================
    if (action === "revokeAdmin") {
        var rEmail  = args.email     || "";
        var rPfId   = args.playFabId || "";
        var rId     = "";

        if (rPfId) {
            rId = rPfId;
        } else if (rEmail) {
            try {
                var rLookup = server.GetUserAccountInfo({ Email: rEmail });
                if (!rLookup || !rLookup.UserInfo) return { success: false, error: "No account found." };
                rId = rLookup.UserInfo.PlayFabId;
            } catch (e) { return { success: false, error: "No account found with that email." }; }
        } else {
            return { success: false, error: "Email or PlayFabId is required." };
        }

        server.UpdateUserData({
            PlayFabId:  rId,
            Data:       { "IsAdmin": "false" },
            Permission: "Public"
        });

        // Sync registry
        var rlist = _readRegistry();
        for (var ri = 0; ri < rlist.length; ri++) {
            if (rlist[ri].playFabId === rId) { rlist[ri].isAdmin = false; break; }
        }
        _saveRegistry(rlist);

        return { success: true, message: "Admin revoked.", playFabId: rId, email: rEmail };
    }

    // ====================================================================================
    // E. BAN USER — accepts email OR playFabId
    // ====================================================================================
    if (action === "banUser") {
        var bEmail = args.email     || "";
        var bPfId  = args.playFabId || "";
        var bId    = "";
        var bName  = "";

        if (bPfId) {
            bId = bPfId;
            try {
                var bInfo = server.GetUserAccountInfo({ PlayFabId: bPfId });
                bName = (bInfo.UserInfo && bInfo.UserInfo.TitleInfo && bInfo.UserInfo.TitleInfo.DisplayName) ? bInfo.UserInfo.TitleInfo.DisplayName : bPfId;
            } catch (e) { bName = bPfId; }
        } else if (bEmail) {
            try {
                var bLookup = server.GetUserAccountInfo({ Email: bEmail });
                if (!bLookup || !bLookup.UserInfo) return { success: false, error: "No account found." };
                bId   = bLookup.UserInfo.PlayFabId;
                bName = (bLookup.UserInfo.TitleInfo && bLookup.UserInfo.TitleInfo.DisplayName) ? bLookup.UserInfo.TitleInfo.DisplayName : bEmail;
            } catch (e) { return { success: false, error: "No account found with that email." }; }
        } else {
            return { success: false, error: "Email or PlayFabId is required." };
        }

        server.UpdateUserData({
            PlayFabId:  bId,
            Data:       { "IsBanned": "true", "IsAdmin": "false" },
            Permission: "Public"
        });

        try { server.BanUsers({ Bans: [{ PlayFabId: bId, Reason: "Banned via Kangi Admin Dashboard", DurationInHours: 87600 }] }); } catch (e) {}

        // Sync registry
        var blist = _readRegistry();
        for (var bi = 0; bi < blist.length; bi++) {
            if (blist[bi].playFabId === bId) { blist[bi].isBanned = true; blist[bi].isAdmin = false; break; }
        }
        _saveRegistry(blist);

        return { success: true, message: "User banned.", playFabId: bId, displayName: bName, email: bEmail };
    }

    // ====================================================================================
    // F. UNBAN USER — accepts email OR playFabId
    // ====================================================================================
    if (action === "unbanUser") {
        var uEmail = args.email     || "";
        var uPfId  = args.playFabId || "";
        var uId    = "";

        if (uPfId) {
            uId = uPfId;
        } else if (uEmail) {
            try {
                var uLookup = server.GetUserAccountInfo({ Email: uEmail });
                if (!uLookup || !uLookup.UserInfo) return { success: false, error: "No account found." };
                uId = uLookup.UserInfo.PlayFabId;
            } catch (e) { return { success: false, error: "No account found with that email." }; }
        } else {
            return { success: false, error: "Email or PlayFabId is required." };
        }

        server.UpdateUserData({
            PlayFabId:  uId,
            Data:       { "IsBanned": "false" },
            Permission: "Public"
        });

        // Sync registry
        var ulist = _readRegistry();
        for (var ui = 0; ui < ulist.length; ui++) {
            if (ulist[ui].playFabId === uId) { ulist[ui].isBanned = false; break; }
        }
        _saveRegistry(ulist);

        return { success: true, message: "User unbanned.", playFabId: uId, email: uEmail };
    }

    return { error: "No matching action found in adminUserWorkflow." };
};
