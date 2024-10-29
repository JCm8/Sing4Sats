window.RoboHashGenerator = {
  generateRoboHash: function (elementId, hashOrValue, size) {    
    const element = document.getElementById(elementId);
    if (element) {
      const imgSrc = `https://robohash.org/${hashOrValue}?set=set4`;
      const imgElements = element.getElementsByTagName('img');
      if (imgElements.length === 0) {
        const imgElement = document.createElement('img');
        imgElement.src = imgSrc;
        imgElement.width = size;
        element.appendChild(imgElement);
      } else {
        imgElements[0].src = imgSrc;
      }
    }
  }
};