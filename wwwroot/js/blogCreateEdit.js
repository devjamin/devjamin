/*
    blogCreateEdit.js, a script for everything but media upload in the create/edit blog.

    - Init Quill (WYSIWYG editor)
    - Set Quill content from hidden input
    - Set hidden input value from Quill content
    - Prevent drop of Base64 images in Quill
    - Generate slug from title
*/
const options = {
    debug: "info",
    modules: {
        toolbar: [[{ header: [1, 2, 3, 4, 5, 6, false] }], ["bold", "italic", "underline", "strike"], [{ list: "ordered" }, { list: "bullet" }], ["blockquote", "code-block"], [{ script: "sub" }, { script: "super" }], [{ indent: "-1" }, { indent: "+1" }], [{ direction: "rtl" }], [{ align: [] }], ["clean"], ["link"]],
        clipboard: {
            matchVisual: false,
        },
    },
    placeholder: "Compose an epic blog...",
    theme: "snow",
    imageDrop: false,
};

var quill = new Quill("#Editor", options);
const contentField = document.getElementById("Content");

if (contentField) {
    quill.root.innerHTML = contentField.value;
}

const form = document.querySelector("form");
if (form) {
    form.addEventListener("submit", function () {
        if (contentField) {
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
