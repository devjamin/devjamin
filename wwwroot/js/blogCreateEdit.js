/*
    blogCreateEdit.js, a script for everything but media upload in the create/edit blog.

    - Init Quill (WYSIWYG editor)
    - Set Quill content from hidden input
    - Set hidden input value from Quill content
    - Prevent drop of Base64 images in Quill
    - Generate slug from title
*/
class QuillCarousel {
    // Keep this in sync with options from blog js
    static carouselOptions = {
        transition: "slide"
    }

    constructor(quill, options) {
        this.quill = quill;
        this.carouselInstances = [];

        // options passed from Quill method
        this.options = options;
        this.container = document.querySelector(options.container);

        // Listen for text changes in Quill and initialize carousels accordingly
        this.quill.on(Quill.events.TEXT_CHANGE, this.initCarousels.bind(this));

        setTimeout(() => {
            this.initCarousels();
        }, 500)
    }

    initCarousels() {
        // Get all carousel containers in the current content
        const carouselContainers = document.querySelectorAll(".f-carousel");

        // Initialize new carousels
        carouselContainers.forEach(container => {

            // Check if this container already has a carousel initialized
            if (!container.dataset.init) {
                // Set a unique data attribute to mark this container as initialized
                container.dataset.init = Math.random().toString(36).substring(7);

                this.fixCarouselBeforeInit();

                // Initialize the Fancyapps carousel
                const carouselNew = new Carousel(container, QuillCarousel.carouselOptions);

                console.info("Init carousel");
                this.carouselInstances.push(carouselNew);

            }
        });

        // cleanup in unused instances by checking if dataset is not in the current content
        this.carouselInstances = this.carouselInstances.filter(instance => {
            return document.querySelector(`[data-init="${instance.container.dataset.init}"]`);
        })
    }

    // Everytime carousel is reinitialized it adds dots, so we remove them first to not have duplicates
    fixCarouselBeforeInit() {
        const dots = document.querySelectorAll(".f-carousel__dots");
        dots.forEach(dot => dot.remove());
    }

    reinitCarousels() {
        this.fixCarouselBeforeInit();

        // Reinitialize all carousels
        this.carouselInstances.forEach(carousel => {
            carousel.reInit();
        })
    }
}

// To make carousel work within Quill, we need to remove added attributes etc. before saving to db
function cleanupCarouselHtmlBeforeSaving() {
    // remove all carousel dots
    const dots = document.querySelectorAll(".f-carousel__dots");
    dots.forEach(dot => dot.remove());
    // remove f-carousel dataset
    const carousels = document.querySelectorAll(".f-carousel");
    carousels.forEach(carousel => {
        delete carousel.dataset.init;
    });
    // remove style transform from f-carousel__track
    const carouselTracks = document.querySelectorAll(".f-carousel__track");
    carouselTracks.forEach(track => {
        track.removeAttribute("style");
    })

    // remove all data-index="0" style="" aria-hidden="true" from all slides
    const carouselSlides = document.querySelectorAll(".f-carousel__slide");
    carouselSlides.forEach(slide => {
        slide.removeAttribute("data-index");
        slide.removeAttribute("style");
        slide.removeAttribute("aria-hidden");
    })
}

Quill.register('modules/carousel', QuillCarousel);

// (() => {
//     // Add slide to blot
//     let blotNode = document.querySelector('.f-carousel__track');
//     blotNode.innerHTML += '<div class="f-carousel__slide is-selected" data-index="0"><img src="https://cdn.devjamin.com/d2d8791e-fe58-4e48-872b-4589b940ec48.jpg"></div>'

//     // grab quill module class method
//     // Access the carousel module instance
//     const carouselModule = quill.getModule('carousel');

//     // Call the reinitCarousels method
//     carouselModule.reinitCarousels();
// })()

async function addMediaAtCursorOrInsideCarousel(filePath, isVideo = false) {
    // Check if the cursor is on the carousel blot
    const range = quill.getSelection();
    if (!range) {
        return false;
    }

    const [blot, offset] = quill.getLeaf(range.index);
    if (!blot || blot.statics.blotName !== 'carousel') {
        // If not inside carousel
        quill.insertEmbed(range.index + 1, isVideo ? "video" : "image", `https://cdn.devjamin.com/${filePath}`);
        return;
    }

    // Find the current slide index
    const currentDot = blot.domNode.querySelector('ul.f-carousel__dots li.is-current');
    if (!currentDot) {
        console.error('No current dot found');
        return;
    }

    const currentIndex = parseInt(currentDot.querySelector('button').getAttribute('data-carousel-page'), 10);

    // Get the blot's track element
    let blotNode = blot.domNode.querySelector('.f-carousel__track');

    // Create the new slide element
    const newSlide = document.createElement('div');
    newSlide.className = 'f-carousel__slide';
    newSlide.setAttribute('data-index', currentIndex + 1);
    newSlide.setAttribute('data-src', `https://cdn.devjamin.com/${filePath}`);
    newSlide.innerHTML = `<img src="https://cdn.devjamin.com/${filePath}">`;

    // Insert the new slide after the current one
    const slides = blotNode.querySelectorAll('.f-carousel__slide');
    if (slides.length > currentIndex + 1) {
        slides[currentIndex].insertAdjacentElement('afterend', newSlide);
    } else {
        blotNode.appendChild(newSlide);
    }

    // Insert the new dot into the carousel dots
    const newDot = document.createElement('li');
    newDot.setAttribute('role', 'presentation');
    newDot.innerHTML = `<button type="button" data-carousel-page="${currentIndex + 1}" aria-label="Go to slide #${currentIndex + 2}" role="tab" aria-selected="false"><span class="f-carousel__dot" aria-hidden="true"></span></button>`;
    currentDot.insertAdjacentElement('afterend', newDot);

    // Update indices for the slides and dots after the new one
    const updateIndices = (nodes, startIndex) => {
        for (let i = startIndex; i < nodes.length; i++) {
            nodes[i].setAttribute('data-index', i);
            const button = nodes[i].querySelector('button');
            if (button) {
                button.setAttribute('data-carousel-page', i);
                button.setAttribute('aria-label', `Go to slide #${i + 1}`);
            }
        }
    };

    updateIndices(blotNode.querySelectorAll('.f-carousel__slide'), currentIndex + 2);
    updateIndices(blot.domNode.querySelectorAll('ul.f-carousel__dots li'), currentIndex + 2);

    // Reinitialize the carousel
    const carouselModule = quill.getModule('carousel');
    carouselModule.reinitCarousels();

    // Slide carousel instance to the new slide
    // Hacky? Maybe, but it works :)
    setTimeout(() => {
        const carouselInstance = carouselModule.carouselInstances.find(instance => instance.container.dataset.init === blot.domNode.dataset.init);

        carouselInstance.slideTo(currentIndex + 1, {
            transition: "fade"
        });

    }, 0)
}

/*
    Custom quill blot for carousel
*/
let BlockEmbed = Quill.import('blots/block/embed');

class CarouselBlot extends BlockEmbed {
    static create(value) {
        let node = super.create();
        node.innerHTML = value;
        return node;
    }

    static value(node) {
        return node.innerHTML;
    }
}

CarouselBlot.blotName = 'carousel';
CarouselBlot.tagName = 'div';
CarouselBlot.className = 'f-carousel';

Quill.register(CarouselBlot);

const options = {
    debug: null, //"info",
    modules: {
        toolbar: [[{ header: [1, 2, 3, 4, 5, 6, false] }], ["bold", "italic", "underline", "strike"], [{ list: "ordered" }, { list: "bullet" }], ["blockquote", "code-block"], [{ script: "sub" }, { script: "super" }], [{ indent: "-1" }, { indent: "+1" }], [{ direction: "rtl" }], [{ align: [] }], ["clean"], ["link"]],
        clipboard: {
            matchVisual: false,
        },
        // Custom module
        carousel: {
            container: '#counter',
            unit: 'word'
        }
    },
    placeholder: "Compose an epic blog...",
    theme: "snow",
    imageDrop: false,
};

var quill = new Quill("#Editor", options);

const contentField = document.getElementById("Content");

if (contentField) {
    cleanupCarouselHtmlBeforeSaving()

    quill.root.innerHTML = contentField.value;
}

// To get carousel working inside quill WYSIWYG editor we need to remove classes etc. appended on initting it
function prepareContentForSaving() {
    // remove all carousel dots
    const dots = document.querySelectorAll(".f-carousel__dots");
    dots.forEach(dot => dot.remove());
    // remove f-carousel dataset
    const carousels = document.querySelectorAll(".f-carousel");
    carousels.forEach(carousel => {
        delete carousel.dataset.init;
    });
    // remove style transform from f-carousel__track
    const carouselTracks = document.querySelectorAll(".f-carousel__track");
    carouselTracks.forEach(track => {
        track.removeAttribute("style");
    })

    // remove all data-index="0" style="" aria-hidden="true" from all slides
    const carouselSlides = document.querySelectorAll(".f-carousel__slide");
    carouselSlides.forEach(slide => {
        slide.removeAttribute("data-index");
        slide.removeAttribute("style");
        slide.removeAttribute("aria-hidden");
    })
}

const form = document.querySelector("form");
if (form) {
    form.addEventListener("submit", function () {
        if (contentField) {
            prepareContentForSaving()

            contentField.value = quill.root.innerHTML;
        }
    });
}

const editorContainer = document.querySelector(".ql-editor");
if (editorContainer) {
    editorContainer.addEventListener("drop", function (e) {
        e.preventDefault();
        setTimeout(() => removeBase64Images(quill), 300);
    });
}

// Periodically check for images and remove them if they are Base64.
// Quill doesn't support disabeling drop of Base64 images, so we have to remove them manually.
setInterval(() => removeBase64Images(quill), 2500);

function removeBase64Images(quill) {
    let imageDropped = false;
    const contents = quill.getContents();
    const ops = [];

    contents.ops.forEach((op) => {
        if (op.insert && op.insert.image && op.insert.image.startsWith("data:image")) {
            imageDropped = true;
        } else {
            ops.push(op); // Keep all other ops
        }
    });

    if (imageDropped) {
        alert("Method not allowed... upload images instead.");
        quill.setContents({ ops }); // Remove Base64 images
    }
}

// Prevent default drop behaviour, so browser doesn't replace window with the file
document.addEventListener(
    "drop",
    function (e) {
        e.preventDefault();
    },
    false
);

// Generate slug on title change and replace in slug input (lowercases and replaces spaces with hyphens)
const titleInput = document.getElementById("Title");
const slugInput = document.getElementById("Slug");

titleInput.oninput = function () {
    const title = titleInput.value;
    const slug = title.toLowerCase().replace(/ /g, "-");
    slugInput.value = slug;
};
