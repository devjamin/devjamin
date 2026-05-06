/*
    Custom Semantic Blots for Better HTML Structure
*/

// Custom Section Blot to wrap related content
const Block = Quill.import('blots/block');

class SectionBlot extends Block {
    static blotName = 'section';
    static tagName = 'section';
    static className = 'articleBody__section';
}

// Custom Article Blot for the main content container
class ArticleBlot extends Block {
    static blotName = 'article';
    static tagName = 'article';
    static className = 'article-content';
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
    articleCreateEdit.js, a script for everything but media upload in the create/edit article.

    - Init Quill (WYSIWYG editor)
    - Set Quill content from hidden input
    - Set hidden input value from Quill content
    - Prevent drop of Base64 images in Quill
    - Generate slug from title
*/
var carouselNode = null;
document.addEventListener('contextmenu', function (event) {
    const target = event.target;

    // Table cell — show table context menu (handled before Quill blot detection
    // because right-clicking inside a cell may not have set the Quill selection)
    const cell = target.closest('.ql-editor td, .ql-editor th');
    if (cell) {
        event.preventDefault();

        // Move Quill caret into the cell so tableModule.* operations have context
        try {
            const cellBlot = Quill.find(cell, true);
            if (cellBlot) {
                const cellIndex = quill.getIndex(cellBlot);
                quill.setSelection(cellIndex, 0, 'silent');
            }
        } catch (e) { /* best-effort */ }

        const tableMenu = document.getElementById('tableContextMenu');
        tableMenu.style.display = 'block';
        tableMenu.style.left = `${event.pageX}px`;
        tableMenu.style.top = `${event.pageY}px`;
        return;
    }

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
    // Check if the blot is a single image
    else if (blot && blot.statics.blotName === 'customImage') {
        event.preventDefault(); // Prevent the default context menu

        // Show the image context menu
        const contextMenu = document.getElementById('imageContextMenu');
        contextMenu.style.display = 'block';
        contextMenu.style.left = `${event.pageX}px`;
        contextMenu.style.top = `${event.pageY}px`;

        // Store the image blot for later use
        contextMenu.imageBlot = blot;
        contextMenu.imageRange = range;
    }
});

// Handle context menu actions
document.getElementById('editCarouselAltText').addEventListener('click', function () {
    const contextMenu = document.getElementById('carouselContextMenu');
    const slideNode = contextMenu.slideNode;

    if (slideNode) {
        const img = slideNode.querySelector('img');
        if (img) {
            // Get current alt text
            const currentAlt = img.getAttribute('alt') || '';

            // Show modal and populate with current alt text
            const modal = document.getElementById('altTextModal');
            const input = document.getElementById('altTextInput');
            input.value = currentAlt;
            modal.style.display = 'flex';
            input.focus();

            // Store the image element in the modal for saving
            modal.carouselImage = img;
        }
    }

    hideContextMenu();
});

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
    const carouselContextMenu = document.getElementById('carouselContextMenu');
    carouselContextMenu.style.display = 'none';
    carouselContextMenu.slideNode = null;
    carouselContextMenu.carouselNode = null;

    const imageContextMenu = document.getElementById('imageContextMenu');
    imageContextMenu.style.display = 'none';
    imageContextMenu.imageBlot = null;
    imageContextMenu.imageRange = null;

    const tableContextMenu = document.getElementById('tableContextMenu');
    if (tableContextMenu) tableContextMenu.style.display = 'none';
}

// Table context menu actions — operate on the cell containing the current selection
function runTableAction(method) {
    const tableModule = quill.getModule('table');
    if (tableModule && typeof tableModule[method] === 'function') {
        tableModule[method]();
    } else {
        console.warn(`Quill table module missing or doesn't expose ${method}().`);
    }
    hideContextMenu();
}

document.addEventListener('DOMContentLoaded', function () {
    const map = {
        tableInsertRowAbove: 'insertRowAbove',
        tableInsertRowBelow: 'insertRowBelow',
        tableInsertColLeft: 'insertColumnLeft',
        tableInsertColRight: 'insertColumnRight',
        tableDeleteRow: 'deleteRow',
        tableDeleteCol: 'deleteColumn',
        tableDeleteTable: 'deleteTable'
    };
    Object.entries(map).forEach(([id, method]) => {
        const el = document.getElementById(id);
        if (el) el.addEventListener('click', () => runTableAction(method));
    });
});

document.addEventListener('click', function () {
    hideContextMenu(); // Hide the context menu on a click outside
});

// Image context menu handlers
document.getElementById('editAltText').addEventListener('click', function () {
    const contextMenu = document.getElementById('imageContextMenu');
    const imageBlot = contextMenu.imageBlot;

    if (imageBlot) {
        // Get current alt text
        const currentAlt = imageBlot.domNode.getAttribute('alt') || '';

        // Show modal and populate with current alt text
        const modal = document.getElementById('altTextModal');
        const input = document.getElementById('altTextInput');
        input.value = currentAlt;
        modal.style.display = 'flex';
        input.focus();

        // Store the image blot in the modal for saving
        modal.imageBlot = imageBlot;
    }

    hideContextMenu();
});

document.getElementById('removeImageSingle').addEventListener('click', function () {
    const contextMenu = document.getElementById('imageContextMenu');
    const imageBlot = contextMenu.imageBlot;
    const range = contextMenu.imageRange;

    if (imageBlot && range) {
        // Remove the image
        quill.deleteText(range.index, 1);
    }

    hideContextMenu();
});

// Alt text modal handlers
document.getElementById('saveAltText').addEventListener('click', function () {
    const modal = document.getElementById('altTextModal');
    const input = document.getElementById('altTextInput');
    const imageBlot = modal.imageBlot;
    const carouselImage = modal.carouselImage;

    if (imageBlot) {
        // Update the alt text for single image
        imageBlot.domNode.setAttribute('alt', input.value);
    } else if (carouselImage) {
        // Update the alt text for carousel image
        carouselImage.setAttribute('alt', input.value);
    }

    modal.style.display = 'none';
    modal.imageBlot = null;
    modal.carouselImage = null;
});

document.getElementById('cancelAltText').addEventListener('click', function () {
    const modal = document.getElementById('altTextModal');
    modal.style.display = 'none';
    modal.imageBlot = null;
    modal.carouselImage = null;
});

// Close modal on escape key
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        const modal = document.getElementById('altTextModal');
        if (modal.style.display === 'flex') {
            modal.style.display = 'none';
            modal.imageBlot = null;
            modal.carouselImage = null;
        }
    }
});

// Close modal when clicking outside
document.getElementById('altTextModal').addEventListener('click', function (e) {
    if (e.target === this) {
        this.style.display = 'none';
        this.imageBlot = null;
        this.carouselImage = null;
    }
});



class QuillCarousel {
    // Keep this in sync with options from article js
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
                const altMatch = imgTag.match(/alt="([^"]*)"/);
                const alt = altMatch ? altMatch[1] : '';
                return `<div class="f-carousel__slide"><img src="${src}" alt="${alt}"></div>`;
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
        quill.insertEmbed(range.index, 'customImage', {
            src: `https://cdn.devjamin.com/${filePath}/w=800,format=auto`,
            alt: ''
        }, 'user');
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
    newSlide.innerHTML = `<img src="https://cdn.devjamin.com/${filePath}/w=800,format=auto" alt="">`;


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
        if (typeof value === 'string') {
            // Legacy support for string values
            node.setAttribute('src', value);
            node.setAttribute('alt', '');
        } else {
            // New object format with src and alt
            node.setAttribute('src', value.src || value);
            node.setAttribute('alt', value.alt || '');
        }
        return node;
    }

    static value(node) {
        return {
            src: node.getAttribute('src'),
            alt: node.getAttribute('alt') || ''
        };
    }

    static formats(node) {
        return {
            alt: node.getAttribute('alt') || ''
        };
    }

    format(name, value) {
        if (name === 'alt') {
            this.domNode.setAttribute('alt', value);
        } else {
            super.format(name, value);
        }
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

const TABLE_PICKER_MAX_ROWS = 10;
const TABLE_PICKER_MAX_COLS = 10;

function insertTable(rows, cols) {
    const tableModule = quill.getModule('table');
    if (tableModule && typeof tableModule.insertTable === 'function') {
        tableModule.insertTable(rows, cols);
        return;
    }
    // Fallback: paste raw HTML if the built-in module isn't wired up
    const range = quill.getSelection() || { index: quill.getLength() };
    let html = '<table><tbody>';
    for (let r = 0; r < rows; r++) {
        html += '<tr>';
        for (let c = 0; c < cols; c++) {
            html += r === 0 ? '<th>Header</th>' : '<td>Cell</td>';
        }
        html += '</tr>';
    }
    html += '</tbody></table>';
    quill.clipboard.dangerouslyPasteHTML(range.index, html, 'user');
}

function buildTableSizePickerGrid() {
    const grid = document.querySelector('#tableSizePicker .tableSizePicker__grid');
    if (!grid || grid.children.length) return grid;
    for (let i = 0; i < TABLE_PICKER_MAX_ROWS * TABLE_PICKER_MAX_COLS; i++) {
        const cell = document.createElement('div');
        cell.className = 'tableSizePicker__cell';
        cell.dataset.row = Math.floor(i / TABLE_PICKER_MAX_COLS) + 1;
        cell.dataset.col = (i % TABLE_PICKER_MAX_COLS) + 1;
        grid.appendChild(cell);
    }
    return grid;
}

function highlightTableSizePicker(rows, cols) {
    const picker = document.getElementById('tableSizePicker');
    if (!picker) return;
    const cells = picker.querySelectorAll('.tableSizePicker__cell');
    cells.forEach((c) => {
        const r = parseInt(c.dataset.row, 10);
        const cc = parseInt(c.dataset.col, 10);
        c.classList.toggle('tableSizePicker__cell--active', r <= rows && cc <= cols);
    });
    const label = picker.querySelector('.tableSizePicker__label');
    if (label) label.textContent = `${rows} × ${cols}`;
}

function hideTableSizePicker() {
    const picker = document.getElementById('tableSizePicker');
    if (picker) picker.style.display = 'none';
    document.removeEventListener('click', onTableSizePickerOutsideClick, true);
}

function onTableSizePickerOutsideClick(e) {
    const picker = document.getElementById('tableSizePicker');
    if (!picker) return;
    if (!picker.contains(e.target) && !e.target.closest('.ql-table')) {
        hideTableSizePicker();
    }
}

function showTableSizePicker(triggerBtn) {
    const picker = document.getElementById('tableSizePicker');
    if (!picker) return;
    const grid = buildTableSizePickerGrid();

    const rect = triggerBtn.getBoundingClientRect();
    picker.style.top = `${rect.bottom + window.scrollY + 4}px`;
    picker.style.left = `${rect.left + window.scrollX}px`;
    picker.style.display = 'block';
    highlightTableSizePicker(3, 3);

    grid.onmousemove = (e) => {
        const cell = e.target.closest('.tableSizePicker__cell');
        if (!cell) return;
        highlightTableSizePicker(parseInt(cell.dataset.row, 10), parseInt(cell.dataset.col, 10));
    };
    grid.onmouseleave = () => highlightTableSizePicker(3, 3);
    grid.onclick = (e) => {
        const cell = e.target.closest('.tableSizePicker__cell');
        if (!cell) return;
        const rows = parseInt(cell.dataset.row, 10);
        const cols = parseInt(cell.dataset.col, 10);
        hideTableSizePicker();
        insertTable(rows, cols);
    };

    // Capture-phase outside-click listener so the picker hides reliably
    setTimeout(() => document.addEventListener('click', onTableSizePickerOutsideClick, true), 0);
}

function insertTableHandler() {
    // The Quill toolbar button click — open the row/col picker positioned under it
    const btn = document.querySelector('.ql-table');
    if (btn) showTableSizePicker(btn);
}

const options = {
    debug: null, //"info",
    modules: {
        toolbar: {
            container: [
                [{ header: [2, 3, 4, 5, 6, false] }], // Removed H1 (1) from options
                ["bold", "italic", "underline", "strike"],
                [{ list: "ordered" }, { list: "bullet" }],
                ["blockquote", "code-block"],
                [{ script: "sub" }, { script: "super" }],
                [{ indent: "-1" }, { indent: "+1" }],
                [{ direction: "rtl" }],
                [{ align: [] }],
                ["clean"],
                ["link"],
                ['table'],
                ['fullscreen']
            ],
            handlers: {
                'fullscreen': toggleFullscreen,
                'table': insertTableHandler
            }
        },
        clipboard: {
            matchVisual: false,
        },
        // Built-in Quill 2 table module — enables row/col helpers via quill.getModule('table')
        table: true,
        // Custom module
        carousel: {
            container: '#counter',
            unit: 'word'
        }
    },
    placeholder: "Compose an epic article...",
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
        'customImage', 'carousel',
        'table', 'table-cell-line', 'table-col'
    ]
};

var quill = new Quill("#Editor", options);

const semanticHTMLField = document.getElementById("SemanticHTML");
const quillDeltaField = document.getElementById("QuillDelta");

if (semanticHTMLField) {
    cleanupCarouselHtmlBeforeSaving()

    // Use QuillDelta for editing if it exists, otherwise fall back to SemanticHTML
    if (quillDeltaField && quillDeltaField.value) {
        try {
            const delta = JSON.parse(quillDeltaField.value);
            quill.setContents(delta);
        } catch (e) {
            console.warn('Failed to parse QuillDelta, falling back to SemanticHTML:', e);
            quill.root.innerHTML = semanticHTMLField.value;
        }
    } else {
        quill.root.innerHTML = semanticHTMLField.value;
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
    This function creates sections based on semantic criteria:
    ✅ Groups multiple paragraphs under one heading
    ✅ Contains subsections (nested headings)
    ✅ Represents a real conceptual unit
*/
function createSemanticStructure(html) {
    // Create a temporary DOM element to work with
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = html;

    // Convert NodeList to Array for easier processing
    const allNodes = Array.from(tempDiv.childNodes);

    let result = '';
    let currentSection = null;
    let currentSectionContent = '';
    let currentSectionLevel = null;

    // Helper function to check if content qualifies for a section
    function shouldCreateSection(content, hasSubheadings = false) {
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = content;

        const paragraphs = tempDiv.querySelectorAll('p');
        const hasMultipleParagraphs = paragraphs.length > 1;
        const hasSubstantialContent = content.trim().length > 100; // Arbitrary threshold for "substantial"

        // Create section if:
        // - Has multiple paragraphs, OR
        // - Has subsections (nested headings), OR
        // - Has substantial content that represents a conceptual unit
        return hasMultipleParagraphs || hasSubheadings || hasSubstantialContent;
    }

    // Helper function to close current section if it qualifies
    function closeCurrentSection() {
        if (currentSection !== null) {
            // Check if we have subsections by looking for headings in content
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = currentSectionContent;
            const hasSubheadings = tempDiv.querySelectorAll('h2, h3, h4, h5, h6').length > 1;

            if (shouldCreateSection(currentSectionContent, hasSubheadings)) {
                result += `<section class="articleBody__section">${currentSectionContent}</section>`;
            } else {
                // Don't wrap in section if it doesn't meet criteria
                result += currentSectionContent;
            }

            currentSection = null;
            currentSectionContent = '';
            currentSectionLevel = null;
        }
    }

    allNodes.forEach((node, index) => {
        if (node.nodeType === Node.ELEMENT_NODE && /^h[2-6]$/i.test(node.tagName)) {
            const level = parseInt(node.tagName.substring(1));

            // If we encounter a heading of same or higher level, close current section
            if (currentSectionLevel !== null && level <= currentSectionLevel) {
                closeCurrentSection();
            }

            // Start new section or add to existing one
            if (currentSection === null) {
                currentSection = level;
                currentSectionLevel = level;
                currentSectionContent = node.outerHTML;
            } else {
                // Add subsection heading to current section
                currentSectionContent += node.outerHTML;
            }

        } else if (node.nodeType === Node.ELEMENT_NODE || (node.nodeType === Node.TEXT_NODE && node.textContent.trim())) {
            // Add content to current section or result
            const content = node.outerHTML || node.textContent;

            if (currentSection !== null) {
                currentSectionContent += content;
            } else {
                result += content;
            }
        }
    });

    // Close any remaining section
    closeCurrentSection();

    // Clean up empty elements before returning
    const finalDiv = document.createElement('div');
    finalDiv.innerHTML = result;

    // Remove empty elements (paragraphs, divs, spans, etc.)
    const removeEmptyElements = (container) => {
        const allElements = container.querySelectorAll('*');

        // Process elements from innermost to outermost (reverse order)
        Array.from(allElements).reverse().forEach(element => {
            // Skip elements that should never be removed even if empty
            const preserveElements = ['img', 'br', 'hr', 'input', 'meta', 'link'];
            const preserveClasses = ['f-carousel', 'f-carousel__slide', 'f-carousel__viewport', 'f-carousel__track'];

            if (preserveElements.includes(element.tagName.toLowerCase()) ||
                preserveClasses.some(className => element.classList.contains(className))) {
                return;
            }

            // Remove if element has no text content and no non-empty children
            const hasTextContent = element.textContent.trim().length > 0;
            const hasNonEmptyChildren = Array.from(element.children).some(child =>
                child.textContent.trim().length > 0 ||
                preserveElements.includes(child.tagName.toLowerCase())
            );

            if (!hasTextContent && !hasNonEmptyChildren) {
                element.remove();
            }
        });
    };

    // Run cleanup multiple times to catch nested empty elements
    removeEmptyElements(finalDiv);
    removeEmptyElements(finalDiv); // Second pass to catch elements that became empty after first pass

    return finalDiv.innerHTML;
}

const form = document.querySelector("form");
if (form) {
    form.addEventListener("submit", function () {
        if (semanticHTMLField) {
            prepareContentForSaving()

            // Save Quill Delta JSON for editing
            const quillDeltaField = document.getElementById("QuillDelta");
            if (quillDeltaField) {
                const delta = quill.getContents();
                quillDeltaField.value = JSON.stringify(delta);
            }

            // Get HTML content directly from editor (includes carousels)
            // Use root.innerHTML instead of getSemanticHTML() to preserve carousel structure
            let semanticHTML = quill.root.innerHTML;
            semanticHTML = createSemanticStructure(semanticHTML);

            // Save processed semantic HTML for display
            semanticHTMLField.value = semanticHTML;
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

// Add fullscreen + table button icons after Quill is initialized
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

    const tableBtn = document.querySelector('.ql-table');
    if (tableBtn && !tableBtn.innerHTML.trim()) {
        tableBtn.innerHTML = `
            <svg viewBox="0 0 18 18">
                <rect class="ql-stroke" x="2" y="3" width="14" height="12" fill="none"></rect>
                <line class="ql-stroke" x1="2" y1="7" x2="16" y2="7"></line>
                <line class="ql-stroke" x1="2" y1="11" x2="16" y2="11"></line>
                <line class="ql-stroke" x1="6" y1="3" x2="6" y2="15"></line>
                <line class="ql-stroke" x1="11" y1="3" x2="11" y2="15"></line>
            </svg>
        `;
        tableBtn.setAttribute('title', 'Insert table — right-click a cell for row/column actions');
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
