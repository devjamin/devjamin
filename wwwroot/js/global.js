class Navigation {
  constructor() {
    this.menuToggle = document.getElementById("menuToggle");
    this.navigationList = document.querySelector(".navigation__list");
    this.menuOpenOverlay = document.getElementById("navigationMenuOpenOverlay")

    this.init();
  }

  init() {
    this.menuToggle.addEventListener("click", () => this.toggleMenu());

    window.addEventListener("resize", () => {
      // If open fix width
      if (this.menuToggle.classList.contains("navigation__menuSvg--open")) {
        // Set transition time to before adjusting width
        this.navigationList.style.transition = "none";
        this.setMenuWidth();
        // Revert transition time temporary change
        setTimeout(() => {
          this.navigationList.style.transition = "0.3s";
        }, 10)
      }
      // If window is bigger than 1024px close menu
      if (window.innerWidth >= 768) {
        this.closeMenu();
      }
    })
  }

  openMenu() {
    this.scrollToTopCallbackAfter(function () {
      this.menuToggle.classList.add("navigation__menuSvg--open");
      this.setMenuWidth();
      this.lockScroll();
      this.showOverlay();
    }.bind(this))
  }

  closeMenu() {
    this.menuToggle.classList.remove("navigation__menuSvg--open");
    this.navigationList.style.width = `0`;
    this.unlockScroll();
    this.hideOverlay();
  }

  lockScroll() {
    document.body.style.overflow = "hidden";
  }

  unlockScroll() {
    document.body.style.overflow = "auto";
  }

  showOverlay() {
    this.menuOpenOverlay.style.display = "block";
    setTimeout(() => {
      this.menuOpenOverlay.style.opacity = "1";
    }, 10)
    this.menuOpenOverlay.addEventListener("click", () => this.closeMenu());
  }

  hideOverlay() {
    this.menuOpenOverlay.style.opacity = "0";
    setTimeout(() => {
      this.menuOpenOverlay.style.display = "none";
    }, 300)
  }

  toggleMenu() {
    if (this.menuToggle.classList.contains("navigation__menuSvg--open")) {
      this.closeMenu();
    } else {
      this.openMenu();
    }
  }

  setMenuWidth() {
    this.navigationList.style.width = `${window.innerWidth / 1.5}px`;
  }

  scrollToTopCallbackAfter(callback) {
    if (window.scrollY > 0) {
      window.scrollTo({ top: 0, behavior: "smooth" });

      // Wait for scroll to top to finish before proceeding
      const onScrollComplete = () => {
        if (window.scrollY === 0) {
          callback();
          window.removeEventListener("scroll", onScrollComplete); // Clean up listener
        }
      };

      window.addEventListener("scroll", onScrollComplete);
    } else {
      callback();
    }
  }
}

new Navigation()

// Prevent's hamburger line transition on load
window.addEventListener("load", function () {
  document.querySelector(".navigation__menuSvg").classList.add("navigation__menuSvg--ready");
});



(() => {
  // Web Share API is available then add event to webShareBtn otherwise remove it
  const webShareBtn = document.getElementById("webShareBtn");

  // Mission abort
  if (!webShareBtn) {
    return;
  }

  if (navigator.share) {
    webShareBtn.addEventListener("click", () => {
      navigator.share({
        title: "@Model.Title",
        text: "@shareText",
        url: window.location.href
      }).catch(error => console.error("Error sharing:", error));
    });
  } else {
    webShareBtn.addEventListener("click", () => {
      navigator.clipboard.writeText(window.location.href).then(() => {
        // todo: show success message
      });
    })
  }
})()

// Frontpage specific
const isFrontpage = window.location.pathname === "/";
if (isFrontpage) {
  // On tab click within name skip email if valid, because contact from hero fills email from that input and no need
  // to go there again
  const nameInput = document.getElementById("Name");
  const emailInput = document.getElementById("Email");
  const messageField = document.getElementById("Message");

  // Regex pattern for a valid email
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  // Function to check if the email is valid
  function isEmailValid(email) {
    return emailRegex.test(email);
  }

  // On tab key click, skip email if valid
  nameInput.addEventListener("keydown", (e) => {
    if (e.key === "Tab") {
      e.preventDefault();
      if (isEmailValid(emailInput.value)) {
        messageField.focus();
      } else {
        emailInput.focus();
      }
    }
  });

}
