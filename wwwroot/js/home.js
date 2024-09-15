document.addEventListener("DOMContentLoaded", function () {
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
