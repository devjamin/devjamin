// query all .f-carousel and init them
const carouselContainers = document.querySelectorAll(".f-carousel");
carouselContainers.forEach(container => {
  const carouselNew = new Carousel(container, {
    transition: "slide"
  });
});

// Fancybox.bind('[data-fancybox="gallery"]', {
//   Carousel: {
//     transition: "slide",
//   },
// });