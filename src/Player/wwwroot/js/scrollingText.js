window.ScrollingText = {
  init: function () {
    const scrollingContainers = document.querySelectorAll('.scrolling-text');
    scrollingContainers.forEach(function (container, index) {
      const text = container.querySelector('.song-title');
      const containerWidth = container.offsetWidth;
      const textWidth = text.scrollWidth;

      if (textWidth > containerWidth) {
        const scrollDistance = textWidth + containerWidth;
        const scrollDuration = scrollDistance / 50; // Adjust speed (50 pixels per second)

        const animationName = `scroll-left-${index}`;

        // Remove existing keyframes if any
        const existingStyle = document.getElementById(animationName);
        if (existingStyle) {
          existingStyle.parentNode.removeChild(existingStyle);
        }

        // Create new style element
        const styleElement = document.createElement('style');
        styleElement.id = animationName;
        styleElement.innerHTML = `
                    @keyframes ${animationName} {
                        0% {
                            transform: translateX(${containerWidth}px);
                        }
                        100% {
                            transform: translateX(-${textWidth}px);
                        }
                    }
                `;
        document.head.appendChild(styleElement);

        text.style.animation = `${animationName} ${scrollDuration}s linear infinite`;
        text.classList.add('scroll-animation');
      } else {
        text.style.animation = '';
        text.classList.remove('scroll-animation');
      }
    });
  }
};

window.addEventListener('resize', function () {
  ScrollingText.init();
});