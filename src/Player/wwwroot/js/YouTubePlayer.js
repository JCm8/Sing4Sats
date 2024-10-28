window.YouTubePlayer = {
  player: null,
  dotNetHelper: null,

  onYouTubeIframeAPIReady: function () {
    // This function is called automatically by the YouTube API
  },

  initialize: function (videoId, dotNetHelper) {
    this.dotNetHelper = dotNetHelper;
    this.player = new YT.Player('player', {
      videoId: videoId,
      playerVars: {
        'autoplay': 1,
        'controls': 1,
        'modestbranding': 1,
        'rel': 0,
        'showinfo': 0,
        'mute': 0,
        'origin': window.location.origin,
        'enablejsapi': 1,
        'widget_referrer': window.location.href,
        'fs': 1,
        'playsinline': 1
      },
      events: {
        'onReady': function (event) {
          event.target.playVideo();
        },
        'onStateChange': function (event) {
          dotNetHelper.invokeMethodAsync('PlayerStateChanged', event.data);
        }
      }
    });
  },

  getDuration: function () {
    return this.player.getDuration();
  },

  getCurrentTime: function () {
    return this.player.getCurrentTime();
  },

  loadVideoById: function (videoId) {
    if (this.player) {
      this.player.loadVideoById({
        videoId: videoId,
        startSeconds: 0,
        suggestedQuality: 'default'
      });
    }
  }
};