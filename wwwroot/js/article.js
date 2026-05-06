// query all .f-carousel and init them
const carouselContainers = document.querySelectorAll(".f-carousel");
carouselContainers.forEach(container => {
  Carousel(container, {
    transition: "slide"
  }, { Arrows, Dots }).init();
});

// Fancybox.bind('[data-fancybox="gallery"]', {
//   Carousel: {
//     transition: "slide",
//   },
// });