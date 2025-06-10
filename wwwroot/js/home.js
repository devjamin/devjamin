document.addEventListener("DOMContentLoaded", function () {
  // Typewriter effect
  const typewriterElement = document.getElementById("typewriter-text");

  const quotes = [
    "Building complex things one bit at a time",
    "Tinkering, improving, iterating",
    "Shipping features, not promises",
    "Fixing that one stupid problem",
  ];

  let currentQuoteIndex = 0;
  let currentCharIndex = quotes[0].length; // Start with first quote fully displayed
  let isDeleting = false;
  let typewriterTimeout;
  let hasStarted = false;

  // Display the first quote immediately
  typewriterElement.textContent = quotes[0];

  function typeWriter() {
    const currentQuote = quotes[currentQuoteIndex];

    if (!hasStarted) {
      // First run - wait before starting to delete
      hasStarted = true;
      isDeleting = true;
      typewriterTimeout = setTimeout(typeWriter, 3500); // Pause before deleting first quote
      return;
    }

    if (isDeleting) {
      // Remove characters
      typewriterElement.textContent = currentQuote.substring(0, currentCharIndex - 1);
      currentCharIndex--;

      if (currentCharIndex === 0) {
        isDeleting = false;
        currentQuoteIndex = (currentQuoteIndex + 1) % quotes.length;
        typewriterTimeout = setTimeout(typeWriter, 500); // Pause before typing next quote
        return;
      }

      typewriterTimeout = setTimeout(typeWriter, 50); // Deleting speed
    } else {
      // Add characters
      typewriterElement.textContent = currentQuote.substring(0, currentCharIndex + 1);
      currentCharIndex++;

      if (currentCharIndex === currentQuote.length) {
        isDeleting = true;
        typewriterTimeout = setTimeout(typeWriter, 2000); // Pause before deleting
        return;
      }

      typewriterTimeout = setTimeout(typeWriter, 100); // Typing speed
    }
  }

  // Start typewriter effect
  typeWriter();

  const arrowLeft = document.getElementById("blogsContainerArrowLeft");
  const arrowRight = document.getElementById("blogsContainerArrowRight");
  const blogsContainer = document.querySelector(".blogs__container");
  const blogs = document.querySelectorAll(".blog");

  const numberOfSlides = blogs.length;
  let activeSlide = 0;

  function updateArrows() {
    if (window.innerWidth <= 1024) {
      arrowLeft.style.display = activeSlide > 0 ? "block" : "none";
      arrowRight.style.display = activeSlide < numberOfSlides - 1 ? "block" : "none";
    } else {
      arrowLeft.style.display = "none";
      arrowRight.style.display = "none";
    }
  }

  // Scroll to the active slide
  function scrollToSlide() {
    const maxScrollLeft = blogsContainer.scrollWidth - blogsContainer.clientWidth;
    const scrollPosition = (activeSlide * maxScrollLeft) / (numberOfSlides - 1);
    blogsContainer.scrollTo({ left: scrollPosition, behavior: "smooth" });
  }

  // Update active slide based on scroll position
  function updateActiveSlide() {
    const maxScrollLeft = blogsContainer.scrollWidth - blogsContainer.clientWidth;
    const currentScroll = blogsContainer.scrollLeft;
    activeSlide = Math.round((currentScroll / maxScrollLeft) * (numberOfSlides - 1));
    updateArrows();
  }

  // Left arrow click event
  arrowLeft.addEventListener("click", () => {
    if (activeSlide > 0) {
      activeSlide--;
      scrollToSlide();
    }
  });

  // Right arrow click event
  arrowRight.addEventListener("click", () => {
    if (activeSlide < numberOfSlides - 1) {
      activeSlide++;
      scrollToSlide();
    }
  });

  window.addEventListener("resize", updateArrows);

  // Update active slide on scroll
  blogsContainer.addEventListener("scroll", updateActiveSlide);

  // Init arrows
  updateArrows();
});
