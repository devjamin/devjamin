const blogPostId = document.getElementById("BlogPostId").value;
const mediaEl = document.getElementById("media");
const addMediaBtn = document.getElementById("addMediaBtn");
const mediaEmptyOverlay = document.getElementById("mediaEmptyOverlay");
const mediaDraggingOverlay = document.getElementById("mediaDraggingOverlay");
const mediaLoadingOverlay = document.getElementById("mediaLoadingOverlay");
const mediaInput = document.getElementById("mediaInput");

function initMediaUpload() {
    if (mediaEl.children.length === 0) {
        mediaEmptyOverlay.style.display = "flex";
    }

    mediaEl.addEventListener("dragover", dragOverHandler);
    mediaEl.addEventListener("dragleave", dragLeaveHandler);
    addMediaBtn.addEventListener("click", () => mediaInput.click());
    mediaEl.addEventListener("drop", (e) => void handleFiles(e.dataTransfer.files));
}

function dragOverHandler(e) {
    e.preventDefault();
    mediaDraggingOverlay.style.display = "flex";
}

function dragLeaveHandler(e) {
    e.preventDefault();
    mediaDraggingOverlay.style.display = "none";
}

async function handleFiles(files) {
    if (!files.length) return;

    const allowedFileTypes = ["image/jpeg", "image/png", "video/mp4"];
    mediaDraggingOverlay.style.display = "none";
    mediaLoadingOverlay.style.display = "flex";

    for (const file of files) {
        if (allowedFileTypes.includes(file.type)) {
            await uploadMedia(file, file.type);
        } else {
            console.warn(`File type '${file.type}' is not allowed.`);
        }
    }
    mediaLoadingOverlay.style.display = "none";
}

async function uploadMedia(file, fileType) {
    const formData = new FormData();
    formData.append("File", file);
    formData.append("BlogPostId", blogPostId);

    try {
        const response = await fetch("/API/Upload-Media", {
            method: "POST",
            body: formData,
            credentials: "same-origin",
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || "An unknown error occurred");
        }

        const data = await response.json();
        addMediaElement(fileType === "video/mp4", data.mediaId, data.filePath);
    } catch (error) {
        alert(error.message);
    }
}

function addMediaElement(isVideo, mediaId, filePath, isCoverImage = false) {
    const mediaElement = document.createElement("div");
    mediaElement.classList.add("ceForm__mediaElement");
    mediaElement.dataset.filePath = filePath;
    mediaElement.dataset.mediaId = mediaId;

    const element = document.createElement(isVideo ? "video" : "img");
    if (isVideo) {
        element.src = `https://devjamin.fra1.cdn.digitaloceanspaces.com/${filePath}`;
        element.controls = true;
    } else {
        element.src = `https://devjamin.fra1.cdn.digitaloceanspaces.com/${filePath}`;
    }

    // Construct mediaElement's innerHTML including the toolbar
    constructMediaElementInnerHTML(mediaElement, element, isVideo, isCoverImage);

    // Add Event Listeners for buttons
    setupMediaElementEventListeners(mediaElement, isVideo, mediaId, filePath);

    mediaEl.appendChild(mediaElement);

    // Hide empty overlay, as we now have items
    mediaEmptyOverlay.style.display = "none";
}

// This function sets up event listeners for the buttons inside mediaElement.
function setupMediaElementEventListeners(mediaElement, isVideo, mediaId, filePath) {
    const addBtn = mediaElement.querySelector(".ceForm__mediaElementToolbarElement--add");
    const coverBtn = mediaElement.querySelector(".ceForm__mediaElementToolbarElement--cover");
    const deleteBtn = mediaElement.querySelector(".ceForm__mediaElementToolbarElement--delete");

    if (addBtn) {
        addBtn.onclick = function () {
            var range = quill.getSelection();

            // No editor selection guard clause
            if (!range) alert("No active selection");

            if (range) {
                quill.insertEmbed(range.index, "image", `https://devjamin.fra1.cdn.digitaloceanspaces.com/${filePath}`);
            }
        };
    }

    if (coverBtn && !isVideo) {
        coverBtn.onclick = async function () {
            const data = {
                BlogPostId: blogPostId,
                MediaId: mediaId,
            };

            try {
                const response = await fetch("/API/Select-Cover-Image", {
                    method: "POST",
                    credentials: "same-origin",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify(data),
                });

                if (!response.ok) {
                    const errorData = await response.json();
                    console.error("Error selecting cover image:", errorData);
                    alert("Error selecting cover image: " + (errorData.Error || "Unknown error"));

                    return;
                }

                const responseData = await response.json();
                console.log("Success:", responseData);

                // Toggle cover image activation
                if (coverBtn.classList.contains("ceForm__mediaElementToolbarElement--coverActive")) {
                    // If clicked button is already active, deactivate it
                    coverBtn.classList.remove("ceForm__mediaElementToolbarElement--coverActive");
                    coverBtn.title = "Make this blog cover image";
                } else {
                    // Deactivate any previously active cover button
                    const activeCoverBtn = document.querySelector(".ceForm__mediaElementToolbarElement--coverActive");
                    if (activeCoverBtn) {
                        activeCoverBtn.classList.remove("ceForm__mediaElementToolbarElement--coverActive");
                        activeCoverBtn.title = "Make this blog cover image";
                    }
                    // Activate clicked cover button
                    coverBtn.classList.add("ceForm__mediaElementToolbarElement--coverActive");
                    coverBtn.title = "Remove this as blog cover image";
                }
            } catch (error) {
                console.error("Fetch error:", error);
                alert("An error occurred while selecting the cover image.");
            }
        };
    }

    if (deleteBtn) {
        deleteBtn.onclick = async function () {
            // Ensure media is not in use in the editor
            const range = quill.getSelection();
            const mediaUrl = `https://devjamin.fra1.cdn.digitaloceanspaces.com/${filePath}`;
            const mediaElementInEditor = quill.root.querySelector(`img[src="${mediaUrl}"]`);
            if (mediaElementInEditor) {
                alert("Media is present in the editor, remove it first.");
                return;
            }

            try {
                const response = await fetch(`/API/Delete-Media/${mediaId}`, {
                    method: "DELETE",
                });

                if (!response.ok) {
                    const errorData = await response.json();
                    console.error("Error deleting media:", errorData);
                    alert(errorData.error || "Unknown error");

                    return;
                }

                const responseData = await response.json();

                mediaElement.remove();

                // Show empty overlay again if no images
                if (mediaEl.children.length === 0) {
                    mediaEmptyOverlay.style.display = "flex";
                }
            } catch (error) {
                console.error("Fetch error:", error);
                alert("An error occurred while deleting the media.");
            }
        };
    }
}

// Constructs the innerHTML of a mediaElement, including the toolbar.
function constructMediaElementInnerHTML(mediaElement, element, isVideo, isCoverImage = false) {
    const coverButtonHTML = !isVideo
        ? `
        <button title="${isCoverImage ? "Remove this as blog cover image" : "Make this blog cover image"}" type="button" class="ceForm__mediaElementToolbarElement ceForm__mediaElementToolbarElement--cover ${isCoverImage ? "ceForm__mediaElementToolbarElement--coverActive" : ""}">
          <svg clip-rule="evenodd" fill-rule="evenodd" stroke-linejoin="round" stroke-miterlimit="2" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="m7.234 3.004c-2.652 0-5.234 1.829-5.234 5.177 0 3.725 4.345 7.727 9.303 12.54.194.189.446.283.697.283s.503-.094.697-.283c4.977-4.831 9.303-8.814 9.303-12.54 0-3.353-2.58-5.168-5.229-5.168-1.836 0-3.646.866-4.771 2.554-1.13-1.696-2.935-2.563-4.766-2.563zm0 1.5c1.99.001 3.202 1.353 4.155 2.7.14.198.368.316.611.317.243 0 .471-.117.612-.314.955-1.339 2.19-2.694 4.159-2.694 1.796 0 3.729 1.148 3.729 3.668 0 2.671-2.881 5.673-8.5 11.127-5.454-5.285-8.5-8.389-8.5-11.127 0-1.125.389-2.069 1.124-2.727.673-.604 1.625-.95 2.61-.95z" fill-rule="nonzero"/></svg>
          </button>
      `
        : "";

    const addButtonHTML = `
          <button title="Add this media at the current editor cursor position" type="button" class="ceForm__mediaElementToolbarElement ceForm__mediaElementToolbarElement--add">
          <svg clip-rule="evenodd" fill-rule="evenodd" stroke-linejoin="round" stroke-miterlimit="2" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="m21 3.998c0-.478-.379-1-1-1h-16c-.62 0-1 .519-1 1v16c0 .621.52 1 1 1h16c.478 0 1-.379 1-1zm-16.5.5h15v15h-15zm6.75 6.752h-3.5c-.414 0-.75.336-.75.75s.336.75.75.75h3.5v3.5c0 .414.336.75.75.75s.75-.336.75-.75v-3.5h3.5c.414 0 .75-.336.75-.75s-.336-.75-.75-.75h-3.5v-3.5c0-.414-.336-.75-.75-.75s-.75.336-.75.75z" fill-rule="nonzero"/></svg>
          </button>
      `;

    const deleteButtonHTML = `
          <button title="Delete this media" type="button" class="ceForm__mediaElementToolbarElement ceForm__mediaElementToolbarElement--delete">
          <svg clip-rule="evenodd" fill-rule="evenodd" stroke-linejoin="round" stroke-miterlimit="2" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="m4.015 5.494h-.253c-.413 0-.747-.335-.747-.747s.334-.747.747-.747h5.253v-1c0-.535.474-1 1-1h4c.526 0 1 .465 1 1v1h5.254c.412 0 .746.335.746.747s-.334.747-.746.747h-.254v15.435c0 .591-.448 1.071-1 1.071-2.873 0-11.127 0-14 0-.552 0-1-.48-1-1.071zm14.5 0h-13v15.006h13zm-4.25 2.506c-.414 0-.75.336-.75.75v8.5c0 .414.336.75.75.75s.75-.336.75-.75v-8.5c0-.414-.336-.75-.75-.75zm-4.5 0c-.414 0-.75.336-.75.75v8.5c0 .414.336.75.75.75s.75-.336.75-.75v-8.5c0-.414-.336-.75-.75-.75zm3.75-4v-.5h-3v.5z" fill-rule="nonzero"/></svg>
          </button>
      `;

    mediaElement.innerHTML = `
          ${element.outerHTML}
          <div class="ceForm__mediaElementToolbar">
              ${coverButtonHTML}
              ${addButtonHTML}
              ${deleteButtonHTML}
          </div>
      `;
}

// Loads existing media from the hidden input field
function loadExistingMedia() {
    const mediaDataInput = document.getElementById("mediaInputExisting");
    if (mediaDataInput) {
        const mediaItems = JSON.parse(mediaDataInput.value);

        mediaItems.forEach((media) => {
            const isVideo = media.filePath.endsWith(".mp4");
            addMediaElement(isVideo, media.mediaId, media.filePath, media.isCoverImage);
        });
    }
}

initMediaUpload();
loadExistingMedia();
