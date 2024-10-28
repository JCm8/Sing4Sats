window.RoboHashGenerator = {
  generateRoboHash: function (elementId, hashOrValue, size) {
    document.getElementById(elementId).src = `https://robohash.org/${hashOrValue}?set=set4`;
  }
};