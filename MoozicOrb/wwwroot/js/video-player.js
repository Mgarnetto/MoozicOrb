/* =========================================
   CUSTOM VIDEO PLAYER CONTROLLER
   Handles all .custom-video-wrapper instances
   ========================================= */

document.addEventListener('DOMContentLoaded', () => {

    // 1. CLICK HANDLING (Play/Pause, Fullscreen, Seek)
    document.body.addEventListener('click', (e) => {
        const wrapper = e.target.closest('.custom-video-wrapper');
        if (!wrapper) return;

        const video = wrapper.querySelector('video');

        // A. BIG OVERLAY BUTTON or VIDEO CLICK
        if (e.target.closest('.video-overlay-play') || e.target.classList.contains('custom-video')) {
            toggleVideo(video, wrapper);
        }

        // B. SMALL TOOLBAR PLAY BUTTON
        if (e.target.closest('.v-play-toggle')) {
            toggleVideo(video, wrapper);
        }

        // C. FULLSCREEN TOGGLE
        if (e.target.closest('.v-fullscreen-toggle')) {
            toggleFullscreen(wrapper);
        }

        // D. SEEK BAR CLICK
        if (e.target.closest('.v-progress-container')) {
            const container = e.target.closest('.v-progress-container');
            const rect = container.getBoundingClientRect();
            const pos = (e.clientX - rect.left) / rect.width;
            video.currentTime = pos * video.duration;
        }
    });

    // 2. VIDEO PROGRESS UPDATE (Time Update)
    // We use "capture" phase to catch events from dynamically added videos
    document.addEventListener('timeupdate', (e) => {
        if (e.target.tagName === 'VIDEO' && e.target.classList.contains('custom-video')) {
            const video = e.target;
            const wrapper = video.closest('.custom-video-wrapper');
            if (wrapper) {
                const percent = (video.currentTime / video.duration) * 100;
                const fill = wrapper.querySelector('.v-progress-fill');
                if (fill) fill.style.width = `${percent}%`;
            }
        }
    }, true);

    // 3. VIDEO ENDED RESET
    document.addEventListener('ended', (e) => {
        if (e.target.tagName === 'VIDEO' && e.target.classList.contains('custom-video')) {
            const wrapper = e.target.closest('.custom-video-wrapper');
            wrapper.classList.remove('playing');
            const btnIcon = wrapper.querySelector('.v-play-toggle i');
            if (btnIcon) btnIcon.className = 'fas fa-play';
        }
    }, true);
});

// --- HELPERS ---

function toggleVideo(video, wrapper) {
    if (video.paused) {
        // Pause all other videos first (Optional - nice UX)
        document.querySelectorAll('video').forEach(v => {
            if (v !== video) {
                v.pause();
                v.closest('.custom-video-wrapper')?.classList.remove('playing');
            }
        });

        video.play();
        wrapper.classList.add('playing');

        // Update small button icon
        const btnIcon = wrapper.querySelector('.v-play-toggle i');
        if (btnIcon) btnIcon.className = 'fas fa-pause';

    } else {
        video.pause();
        wrapper.classList.remove('playing');

        const btnIcon = wrapper.querySelector('.v-play-toggle i');
        if (btnIcon) btnIcon.className = 'fas fa-play';
    }
}

function toggleFullscreen(wrapper) {
    if (!document.fullscreenElement) {
        if (wrapper.requestFullscreen) wrapper.requestFullscreen();
        else if (wrapper.webkitRequestFullscreen) wrapper.webkitRequestFullscreen();
    } else {
        if (document.exitFullscreen) document.exitFullscreen();
    }
}