/*
    site.js, shared behaviour for all public pages (no dependencies).
    - Theme toggle (light/dark, persisted)
    - Nav hairline on scroll + mobile menu
    - Email de-obfuscation
    - Article share button
*/

// Theme toggle
document.getElementById("themeToggle")?.addEventListener("click", () => {
  const root = document.documentElement;
  root.dataset.theme = root.dataset.theme === "dark" ? "light" : "dark";
  try {
    localStorage.setItem("dj-theme", root.dataset.theme);
  } catch (e) { }
});

// Nav: hairline once scrolled (border-color only, no size change)
const nav = document.getElementById("nav");
const onScroll = () => nav.classList.toggle("is-scrolled", window.scrollY > 24);
window.addEventListener("scroll", onScroll, { passive: true });
onScroll();

// Mobile menu
const menuToggle = document.getElementById("menuToggle");
const setMenu = open => {
  nav.classList.toggle("is-open", open);
  menuToggle.setAttribute("aria-expanded", open);
  menuToggle.setAttribute("aria-label", open ? "Close menu" : "Open menu");
};
menuToggle.addEventListener("click", () => setMenu(!nav.classList.contains("is-open")));
document.querySelectorAll("#mobileNav a").forEach(a => a.addEventListener("click", () => setMenu(false)));
document.addEventListener("keydown", e => {
  if (e.key === "Escape" && nav.classList.contains("is-open")) {
    setMenu(false);
    menuToggle.focus();
  }
});
window.addEventListener("resize", () => {
  if (window.innerWidth > 760) setMenu(false);
});

// Decode obfuscated emails (data-email = hex: first byte is the XOR key, rest is the encoded address)
document.querySelectorAll("[data-email]").forEach(el => {
  const bytes = el.dataset.email.match(/../g).map(h => parseInt(h, 16));
  const email = bytes.slice(1).map(b => String.fromCharCode(b ^ bytes[0])).join("");
  el.href = `mailto:${email}`;
  if (!el.hasAttribute("data-email-keep-text")) el.textContent = email;
});

// Article share: native share sheet where available, otherwise copy the link
(() => {
  const shareBtn = document.getElementById("shareBtn");
  if (!shareBtn) return;

  const tip = shareBtn.querySelector(".copy-tip");
  shareBtn.addEventListener("click", async () => {
    const data = { title: document.title, url: window.location.href };
    if (navigator.share) {
      try {
        await navigator.share(data);
      } catch (e) { }
      return;
    }
    try {
      await navigator.clipboard.writeText(data.url);
      tip.classList.add("is-on");
      setTimeout(() => tip.classList.remove("is-on"), 2000);
    } catch (e) { }
  });
})();
