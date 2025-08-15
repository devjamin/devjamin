/*
    Custom Semantic Blots for Better HTML Structure
*/

// Custom Section Blot to wrap related content
const Block = Quill.import('blots/block');

class SectionBlot extends Block {
    static blotName = 'section';
    static tagName = 'section';
    static className = 'blogBody__section';
}

// Custom Article Blot for the main content container
class ArticleBlot extends Block {
    static blotName = 'article';
    static tagName = 'article';
    static className = 'blog-article';
}

// Enhanced Header Blot with better semantic structure
class SemanticHeaderBlot extends Block {
    static blotName = 'semantic-header';
    static tagName = ['H1', 'H2', 'H3', 'H4', 'H5', 'H6'];

    static create(value) {
        const level = value?.level || 2; // Default to H2
        const node = document.createElement(`H${level}`);
        return node;
    }

    static formats(node) {
        const level = parseInt(node.tagName.substring(1));
        return { level };
    }

    format(name, value) {
        if (name === 'semantic-header' && value?.level) {
            const newNode = SemanticHeaderBlot.create(value);
            newNode.innerHTML = this.domNode.innerHTML;
            this.domNode.parentNode.replaceChild(newNode, this.domNode);
            this.domNode = newNode;
        } else {
            super.format(name, value);
        }
    }
}

// Register the semantic blots
Quill.register(SectionBlot);
Quill.register(ArticleBlot);
Quill.register(SemanticHeaderBlot);

// Import and register the List format to ensure bullet lists work properly
const List = Quill.import('formats/list');
Quill.register(List, true);

/*
    blogCreateEdit.js, a script for everything but media upload in the create/edit blog.

    - Init Quill (WYSIWYG editor)
    - Set Quill content from hidden input
    - Set hidden input value from Quill content
    - Prevent drop of Base64 images in Quill
    - Generate slug from title
*/
var carouselNode = null;
document.addEventListener('contextmenu', function (event) {
    const target = event.target;

    // Check if the cursor is on the carousel blot
    const range = quill.getSelection();
    if (!range) {
        return false;
    }

    const [blot, offset] = quill.getLeaf(range.index);

    // Check if the blot is a carousel
    if (blot && blot.statics.blotName === 'carousel') {
        event.preventDefault(); // Prevent the default context menu

        // Get the carousel DOM node
        carouselNode = blot.domNode.querySelector('.f-carousel__slide.is-selected');

        // Check if the clicked target is inside a slide
        const slideNode = target.closest('.f-carousel__slide');

        if (slideNode) {
            // Show the custom context menu
            const contextMenu = document.getElementById('carouselContextMenu');
            contextMenu.style.display = 'block';
            contextMenu.style.left = `${event.pageX}px`;
            contextMenu.style.top = `${event.pageY}px`;

            // Store the slide node and carousel node for later use
            contextMenu.slideNode = slideNode;
            contextMenu.carouselNode = carouselNode;
        }
    }
});

// Handle context menu actions
document.getElementById('moveLeft').addEventListener('click', function () {
    const contextMenu = document.getElementById('carouselContextMenu');
    const slideNode = contextMenu.slideNode;
    const carouselNode = contextMenu.carouselNode;

    if (slideNode && carouselNode) {
        const prevSlide = slideNode.previousElementSibling;

        destroyCarousels()

        if (prevSlide && prevSlide.classList.contains('f-carousel__slide')) {
            slideNode.parentNode.insertBefore(slideNode, prevSlide);
        }
        hideContextMenu();

        // Reinitialize the carousel
        const carouselModule = quill.getModule('carousel');
        carouselModule.reinitCarousels();
    }
});


document.getElementById('moveRight').addEventListener('click', function () {
    const contextMenu = document.getElementById('carouselContextMenu');
    const slideNode = contextMenu.slideNode;

    if (slideNode) {
        const nextSlide = slideNode.nextElementSibling;

        destroyCarousels()

        if (nextSlide && nextSlide.classList.contains('f-carousel__slide')) {
            slideNode.parentNode.insertBefore(nextSlide, slideNode);
        }
        hideContextMenu();

        // Reinitialize the carousel
        const carouselModule = quill.getModule('carousel');
        carouselModule.reinitCarousels();
    }
});

function destroyCarousels() {
    const carouselModule = quill.getModule('carousel');

    console.log("destroyCarousels", carouselModule.carouselInstances)

    carouselModule.carouselInstances.forEach(instance => {
        instance.destroy();
    })


    carouselModule.carouselInstances = [];

    cleanupCarouselHtmlBeforeSaving()

    document.querySelectorAll("[data-init]").forEach(container => {
        delete container.dataset.init;
        container.classList.add("f-carousel");
    })
}

document.getElementById('removeImage').addEventListener('click', function () {
    const contextMenu = document.getElementById('carouselContextMenu');
    const slideNode = contextMenu.slideNode;

    destroyCarousels()

    if (slideNode) {
        slideNode.remove();
        hideContextMenu();

        // Reinitialize the carousel
        const carouselModule = quill.getModule('carousel');

        checkAndConvertToImage()

        carouselModule.reinitCarousels();
    }
});

function hideContextMenu() {
    const contextMenu = document.getElementById('carouselContextMenu');
    contextMenu.style.display = 'none';
    contextMenu.slideNode = null;
    contextMenu.carouselNode = null;
}

document.addEventListener('click', function () {
    hideContextMenu(); // Hide the context menu on a click outside
});



class QuillCarousel {
    // Keep this in sync with options from blog js
    static carouselOptions = {
        transition: "slide",
        Arrows: {
            enabled: true
        }
    }

    constructor(quill, options) {
        this.quill = quill;
        this.carouselInstances = [];

        // options passed from Quill method
        this.options = options;
        this.container = document.querySelector(options.container);

        // Listen for text changes in Quill and initialize carousels accordingly
        //this.quill.on(Quill.events.TEXT_CHANGE, this.initCarousels.bind(this));

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
                console.info("Init carousel", container);

                // Set a unique data attribute to mark this container as initialized
                container.dataset.init = Math.random().toString(36).substring(7);

                this.fixCarouselDotsBeforeInit();

                // Initialize the Fancyapps carousel
                const carouselNew = Carousel(container, QuillCarousel.carouselOptions, { Arrows, Dots }).init();

                console.log("pushing new carousel")

                this.carouselInstances.push(carouselNew);

            }
        });
    }

    // Everytime carousel is reinitialized it adds dots, so we remove them first to not have duplicates
    fixCarouselDotsBeforeInit() {
        const dots = document.querySelectorAll(".f-carousel__dots");
        dots.forEach(dot => dot.remove());
    }

    reinitCarousels() {
        console.info("Reinit carousels");
        // Remove data-init attribute from all carousels, so initCarousels knows to reinitialize them
        const carouselContainers = document.querySelectorAll(".f-carousel__viewport");
        carouselContainers.forEach(container => {
            delete container.parentElement.dataset.init;
            container.parentElement.classList.add("f-carousel");
        })

        // Reinitialize all carousels
        this.carouselInstances.forEach(carousel => {
            carousel.destroy();
        })
        this.carouselInstances = [];


        this.initCarousels();
    }
}

// Prevent key events inside carousel
document.addEventListener('keydown', function (e) {
    // check if cursor inside carousel
    const range = quill.getSelection();
    if (!range) {
        return false;
    }

    const [blot, offset] = quill.getLeaf(range.index);
    // Check if the blot is a carousel
    if (blot && blot.statics.blotName === 'carousel') {
        if (!e.key.includes("Arrow")) {
            e.preventDefault()
        }
    }
})

// To make carousel work within Quill, we need to remove added attributes etc. before saving to db
function cleanupCarouselHtmlBeforeSaving() {
    // remove all carousel dots
    const dots = document.querySelectorAll(".f-carousel__dots");
    dots.forEach(dot => dot.remove());
    // remove f-carousel dataset
    const carousels = document.querySelectorAll("[data-init]");
    carousels.forEach(carousel => {
        delete carousel.dataset.init;
        carousel.classList.add("f-carousel");
    });

    // remove all data-index="0" style="" aria-hidden="true" from all slides
    const carouselSlides = document.querySelectorAll(".f-carousel__slide");
    carouselSlides.forEach(slide => {
        slide.removeAttribute("data-index");
        slide.removeAttribute("style");
        slide.removeAttribute("aria-hidden");
    })

    const carouselButtons = document.querySelectorAll(".f-carousel__button");
    carouselButtons.forEach(button => button.remove());
}

Quill.register('modules/carousel', QuillCarousel);

function checkAndConvertToCarousel() {
    // Get the editor's HTML content
    const editor = document.querySelector('.ql-editor');
    const htmlContent = editor.innerHTML;

    // Use a regular expression to find consecutive <img> tags
    const consecutiveImagesPattern = /(<img [^>]*src="[^"]*"[^>]*>)(\s*<img [^>]*src="[^"]*"[^>]*>)+/g;

    // Check if there are consecutive images
    const matches = htmlContent.match(consecutiveImagesPattern);

    if (matches) {
        matches.forEach(match => {
            // Replace consecutive <img> tags with a carousel structure
            const images = match.match(/<img [^>]*src="[^"]*"[^>]*>/g);
            const carouselContent = images.map(imgTag => {
                const src = imgTag.match(/src="([^"]*)"/)[1];
                return `<div class="f-carousel__slide"><img src="${src}"></div>`;
            }).join('');

            const carouselWrapper = `<div class="f-carousel">${carouselContent}</div>`;

            // Replace the original consecutive images with the carousel HTML
            editor.innerHTML = editor.innerHTML.replace(match, carouselWrapper);
        });

        // Reinitialize carousels if needed
        const carouselModule = quill.getModule('carousel');
        if (carouselModule) {
            carouselModule.reinitCarousels();
        }
    }
}

function checkAndConvertToImage() {
    // Get the editor's HTML content
    const editor = document.querySelector('.ql-editor');
    const carousels = editor.querySelectorAll('.f-carousel');

    carousels.forEach(carousel => {
        const slides = carousel.querySelectorAll('.f-carousel__slide');
        if (slides.length === 1) {
            const imgSrc = slides[0].querySelector('img').getAttribute('src');
            const imgTag = `<img src="${imgSrc}" />`;

            // Replace the carousel with the single image tag
            carousel.outerHTML = imgTag;
        }
    });

    const carouselModule = quill.getModule('carousel');
    if (carouselModule) {
        carouselModule.reinitCarousels();
    }
}




async function addMediaAtCursorOrInsideCarousel(filePath, isVideo = false) {
    // Check if the cursor is on the carousel blot
    const range = quill.getSelection();
    if (!range) {
        return false;
    }

    const [blot, offset] = quill.getLeaf(range.index);
    if (!blot || blot.statics.blotName !== 'carousel') {
        // If not inside carousel, we want just single image to be added at cursor position
        quill.insertEmbed(range.index, 'customImage', `https://cdn.devjamin.com/${filePath}/w=800,format=auto`, 'user');
        quill.setSelection(range.index + 1, 0); // Move cursor after the media

        checkAndConvertToCarousel(range.index);
        return;
    }

    // Find the current slide index
    const currentDot = blot.domNode.querySelector('.f-carousel__dots .is-selected');
    if (!currentDot) {
        console.error('No current dot found');
        return;
    }

    const currentIndex = parseInt(currentDot.getAttribute('data-carousel-go-to'), 10);



    // Get the blot's track element
    let blotNode = blot.domNode.querySelector('.f-carousel__viewport');

    // Create the new slide element
    const newSlide = document.createElement('div');
    newSlide.className = 'f-carousel__slide';
    newSlide.setAttribute('data-index', currentIndex + 1);
    newSlide.setAttribute('data-src', `https://cdn.devjamin.com/${filePath}/w=800,format=auto`);
    newSlide.innerHTML = `<img src="https://cdn.devjamin.com/${filePath}/w=800,format=auto">`;


    destroyCarousels()

    // Insert the new slide after the current one
    const slides = blotNode.querySelectorAll('.f-carousel__slide');
    if (slides.length > currentIndex + 1) {
        slides[currentIndex].insertAdjacentElement('afterend', newSlide);
    } else {
        blotNode.appendChild(newSlide);
    }

    // Insert the new dot into the carousel dots
    const newDot = document.createElement('button');
    newDot.classList.add('f-carousel__dot');
    newDot.setAttribute('data-carousel-go-to', currentIndex + 1);
    newDot.setAttribute('aria-label', `Go to slide #${currentIndex + 2}`);
    currentDot.insertAdjacentElement('afterend', newDot);

    // Update indices for the slides and dots after the new one
    const updateIndices = (nodes, startIndex) => {
        for (let i = startIndex; i < nodes.length; i++) {
            nodes[i].setAttribute('data-index', i);
            const button = nodes[i].querySelector('button');
            if (button) {
                button.setAttribute('data-carousel-go-to', i);
                button.setAttribute('aria-label', `Go to slide #${i + 1}`);
            }
        }
    };

    updateIndices(blotNode.querySelectorAll('.f-carousel__slide'), currentIndex + 2);
    updateIndices(blot.domNode.querySelectorAll('ul.f-carousel__dots li'), currentIndex + 2);

    // Reinitialize the carousel
    const carouselModule = quill.getModule('carousel');
    carouselModule.reinitCarousels();
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

class ImageBlot extends BlockEmbed {
    static create(value) {
        let node = super.create();
        node.setAttribute('src', value);
        return node;
    }

    static value(node) {
        return node.getAttribute('src');
    }
}

ImageBlot.blotName = 'customImage';
ImageBlot.tagName = 'img';

// Register the blot with Quill
Quill.register(ImageBlot);

// Custom fullscreen handler
function toggleFullscreen() {
    const editor = document.querySelector('.ceForm__editor');
    const toolbar = document.querySelector('.ql-toolbar');
    const body = document.body;

    if (editor.classList.contains('quill-fullscreen')) {
        // Exit fullscreen
        editor.classList.remove('quill-fullscreen');
        toolbar.classList.remove('quill-fullscreen-toolbar');
        body.classList.remove('quill-fullscreen-active');

        // Update button icon to expand
        const fullscreenBtn = document.querySelector('.ql-fullscreen');
        if (fullscreenBtn) {
            fullscreenBtn.innerHTML = `
                <svg viewBox="0 0 18 18">
                    <polyline class="ql-stroke" points="5,5 1,5 1,1 5,1"></polyline>
                    <polyline class="ql-stroke" points="13,5 17,5 17,1 13,1"></polyline>
                    <polyline class="ql-stroke" points="13,13 17,13 17,17 13,17"></polyline>
                    <polyline class="ql-stroke" points="5,13 1,13 1,17 5,17"></polyline>
                </svg>
            `;
        }
    } else {
        // Enter fullscreen
        editor.classList.add('quill-fullscreen');
        toolbar.classList.add('quill-fullscreen-toolbar');
        body.classList.add('quill-fullscreen-active');

        // Update button icon to compress
        const fullscreenBtn = document.querySelector('.ql-fullscreen');
        if (fullscreenBtn) {
            fullscreenBtn.innerHTML = `
                <svg viewBox="0 0 18 18">
                    <polyline class="ql-stroke" points="5,1 5,5 1,5"></polyline>
                    <polyline class="ql-stroke" points="13,1 13,5 17,5"></polyline>
                    <polyline class="ql-stroke" points="13,17 13,13 17,13"></polyline>
                    <polyline class="ql-stroke" points="5,17 5,13 1,13"></polyline>
                </svg>
            `;
        }
    }
}

const options = {
    debug: null, //"info",
    modules: {
        toolbar: {
            container: [
                [{ header: [1, 2, 3, 4, 5, 6, false] }],
                ["bold", "italic", "underline", "strike"],
                [{ list: "ordered" }, { list: "bullet" }],
                ["blockquote", "code-block"],
                [{ script: "sub" }, { script: "super" }],
                [{ indent: "-1" }, { indent: "+1" }],
                [{ direction: "rtl" }],
                [{ align: [] }],
                ["clean"],
                ["link"],
                ['fullscreen']
            ],
            handlers: {
                'fullscreen': toggleFullscreen
            }
        },
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
    // Enable semantic HTML output
    formats: [
        'bold', 'italic', 'underline', 'strike',
        'header', 'semantic-header',
        'list',
        'blockquote', 'code-block',
        'script', 'indent', 'direction', 'align',
        'link', 'image', 'video',
        'section', 'article',
        'customImage', 'carousel'
    ]
};

var quill = new Quill("#Editor", options);

const contentField = document.getElementById("Content");
const quillContentField = document.getElementById("QuillContent");

if (contentField) {
    cleanupCarouselHtmlBeforeSaving()

    // Use QuillContent for editing if it exists, otherwise fall back to Content
    if (quillContentField && quillContentField.value) {
        quill.root.innerHTML = quillContentField.value;
    } else {
        quill.root.innerHTML = contentField.value;
    }
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

/*
    Post-process HTML to add semantic structure
    This function automatically wraps headings and their following content in semantic sections
    with proper hierarchical nesting based on heading levels
*/
function createSemanticStructure(html) {
    // Create a temporary DOM element to work with
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = html;

    // Convert NodeList to Array for easier processing
    const allNodes = Array.from(tempDiv.childNodes);

    let result = '';
    let sectionStack = []; // Stack to track nested sections: {level, content}

    // Helper function to close sections down to a specific level
    function closeSectionsToLevel(targetLevel) {
        while (sectionStack.length > 0 && sectionStack[sectionStack.length - 1].level >= targetLevel) {
            const section = sectionStack.pop();
            const closedSection = `<section class="blogBody__section">${section.content}</section>`;

            if (sectionStack.length > 0) {
                // Add closed section to parent section
                sectionStack[sectionStack.length - 1].content += closedSection;
            } else {
                // Add closed section to final result
                result += closedSection;
            }
        }
    }

    // Helper function to add content to the deepest open section or result
    function addContent(content) {
        if (sectionStack.length > 0) {
            sectionStack[sectionStack.length - 1].content += content;
        } else {
            result += content;
        }
    }

    allNodes.forEach((node, index) => {
        if (node.nodeType === Node.ELEMENT_NODE && /^h[1-6]$/i.test(node.tagName)) {
            const level = parseInt(node.tagName.substring(1));

            // Close sections that are at the same level or deeper
            closeSectionsToLevel(level);

            // Start new section
            const newSection = {
                level: level,
                content: node.outerHTML
            };

            // Push the new section to the stack
            sectionStack.push(newSection);

        } else if (node.nodeType === Node.ELEMENT_NODE || (node.nodeType === Node.TEXT_NODE && node.textContent.trim())) {
            // Add content to current section or result
            const content = node.outerHTML || node.textContent;
            addContent(content);
        }
    });

    // Close all remaining sections
    closeSectionsToLevel(0);

    // Remove empty paragraph tags before returning
    const finalDiv = document.createElement('div');
    finalDiv.innerHTML = result;

    // Find and remove empty <p> tags
    const emptyParagraphs = finalDiv.querySelectorAll('p');
    emptyParagraphs.forEach(p => {
        if (!p.textContent.trim() && p.children.length === 0) {
            p.remove();
        }
    });

    // Wrap everything in an article
    return finalDiv.innerHTML
}

const form = document.querySelector("form");
if (form) {
    form.addEventListener("submit", function () {
        if (contentField) {
            prepareContentForSaving()

            // Save raw Quill content for editing
            const quillContentField = document.getElementById("QuillContent");
            if (quillContentField) {
                quillContentField.value = quill.root.innerHTML;
            }

            // Get semantic HTML and apply post-processing for better structure
            let semanticHTML = quill.getSemanticHTML();
            semanticHTML = createSemanticStructure(semanticHTML);

            // Save processed semantic HTML for display
            contentField.value = semanticHTML;
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

// Generate URL-friendly slug from title
const titleInput = document.getElementById("Title");
const slugInput = document.getElementById("Slug");
titleInput.oninput = function () {
    const title = titleInput.value;
    const slug = title
        .replace(/[^a-zA-Z0-9]+/g, '-') // Replace any non-alphanumeric characters with a single hyphen
        .replace(/^-+|-+$/g, '') // Remove leading and trailing hyphens
        .trim(); // Remove any whitespace
    slugInput.value = slug;
};

// Remove validation errors on input
document.addEventListener('DOMContentLoaded', function () {
    // Get all inputs and textareas
    const formInputs = document.querySelectorAll('.ceForm__input, .ceForm__textarea');

    formInputs.forEach(input => {
        input.addEventListener('input', function () {
            // Find associated error span
            const errorSpan = this.nextElementSibling;
            if (errorSpan && errorSpan.classList.contains('ceForm__error')) {
                errorSpan.remove();
            }
        });
    });
});

// Add fullscreen button icon after Quill is initialized
setTimeout(() => {
    const fullscreenBtn = document.querySelector('.ql-fullscreen');
    if (fullscreenBtn) {
        fullscreenBtn.innerHTML = `
            <svg viewBox="0 0 18 18">
                <polyline class="ql-stroke" points="5,5 1,5 1,1 5,1"></polyline>
                <polyline class="ql-stroke" points="13,5 17,5 17,1 13,1"></polyline>
                <polyline class="ql-stroke" points="13,13 17,13 17,17 13,17"></polyline>
                <polyline class="ql-stroke" points="5,13 1,13 1,17 5,17"></polyline>
            </svg>
        `;
        fullscreenBtn.setAttribute('title', 'Toggle fullscreen');
    }
}, 100);

// Handle ESC key to exit fullscreen
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        e.preventDefault();
        const editor = document.querySelector('.ceForm__editor');
        const toolbar = document.querySelector('.ql-toolbar');
        if (editor && editor.classList.contains('quill-fullscreen') &&
            toolbar && toolbar.classList.contains('quill-fullscreen-toolbar')) {
            toggleFullscreen();
        }
    }
});
