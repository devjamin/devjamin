/*
    home.js, homepage behaviour.
    - Contact form (validation + POST /API/Contact), works without GSAP
    - GSAP: draggable hero stickers, short hero intro, pinned Work carousel (desktop),
      one-time section reveals. Skipped for prefers-reduced-motion.
*/

// Contact form
(() => {
  const form = document.getElementById("contactForm");
  if (!form) return;

  const button = document.getElementById("contactFormBtn");
  const label = button.querySelector(".send__lbl");
  const status = document.getElementById("formStatus");
  const fields = [form.Name, form.Email, form.Message];

  const setStatus = (text, type) => {
    status.textContent = text;
    status.classList.toggle("is-error", type === "error");
    status.classList.toggle("is-ok", type === "ok");
  };

  const showError = (input, message) => {
    document.getElementById(`${input.id}Error`).textContent = message;
    input.setAttribute("aria-invalid", message ? "true" : "false");
  };

  // Clear a field's error as soon as it's edited
  fields.forEach(input => input.addEventListener("input", () => showError(input, "")));

  form.addEventListener("submit", async e => {
    e.preventDefault();
    if (button.disabled) return;
    setStatus("");

    // Built-in browser validation, shown inline
    let firstInvalid = null;
    fields.forEach(input => {
      const valid = input.validity.valid;
      showError(input, valid ? "" : input.validationMessage);
      if (!valid && !firstInvalid) firstInvalid = input;
    });
    if (firstInvalid) {
      firstInvalid.focus();
      return;
    }

    button.disabled = true;
    button.classList.add("is-loading");
    label.textContent = "Sending";

    try {
      const response = await fetch("/API/Contact", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          Name: form.Name.value,
          From: form.Email.value,
          Subject: "Contact form submission",
          Body: form.Message.value,
          Website: form.Website.value
        })
      });

      if (!response.ok) {
        if (response.status === 429) throw new Error("I'm not gonna read that fast! Slow down a bit!");
        throw new Error((await response.text()) || "Failed to send message. Please try again.");
      }

      const firstName = form.Name.value.trim().split(" ")[0];
      button.classList.remove("is-loading");
      button.classList.add("is-sent");
      label.textContent = "Sent";
      setStatus(`Thanks for reaching out ${firstName}! I'll get back to you as soon as possible.`, "ok");
      form.reset();
      setTimeout(() => {
        button.classList.remove("is-sent");
        label.textContent = "Send message";
        button.disabled = false;
      }, 3500);
    } catch (error) {
      console.error(error);
      button.classList.remove("is-loading");
      label.textContent = "Send message";
      button.disabled = false;
      setStatus(error.message, "error");
    }
  });
})();

// Motion (GSAP). The page is fully styled and visible without it.
(() => {
  if (!window.gsap) return;
  const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  gsap.registerPlugin(ScrollTrigger, Draggable);

  // Draggable stickers: user-initiated, so kept with reduced motion (just without the lift tween)
  Draggable.create(".sticker", {
    type: "x,y",
    bounds: document.getElementById("hero"),
    edgeResistance: 0.8,
    zIndexBoost: true,
    onPress() {
      this.target.classList.add("is-grab");
      if (!reduce) gsap.to(this.target, { scale: 1.06, duration: 0.2, ease: "power2.out", overwrite: "auto" });
    },
    onRelease() {
      this.target.classList.remove("is-grab");
      if (!reduce) gsap.to(this.target, { scale: 1, duration: 0.35, ease: "power2.out", overwrite: "auto" });
    }
  }).forEach(d => {
    // Double-click: return to the original spot
    d.target.addEventListener("dblclick", () => {
      gsap.to(d.target, { x: 0, y: 0, scale: 1, duration: reduce ? 0 : 0.6, ease: "power3.out", onComplete: () => d.update() });
    });
  });

  if (reduce) return;

  // Hero intro: <= 1.2s, text readable almost at once
  gsap.timeline({ defaults: { ease: "power2.out" } })
    .from(".h-in", { y: 16, opacity: 0, duration: 0.6, stagger: 0.06 }, 0)
    .from(".portrait, .pzone__bg", { y: 16, opacity: 0, duration: 0.7 }, 0.1)
    .from(".sticker", { scale: 0.85, opacity: 0, duration: 0.45, stagger: 0.06, ease: "back.out(1.6)" }, 0.45)
    .from(".me-note", { x: -8, opacity: 0, duration: 0.4 }, 0.8);

  const start = () => {
    // Work: pinned horizontal carousel on desktop. Vertical scroll drives the track sideways,
    // scrub: 1 adds a short catch-up so it glides. Phones and short screens keep the normal stack.
    const work = document.getElementById("work");
    const track = work.querySelector(".projects");
    const count = document.getElementById("workCount");
    const bar = document.getElementById("workBar");
    const cards = track.children.length;
    gsap.matchMedia().add("(min-width: 901px) and (min-height: 600px)", () => {
      work.classList.add("is-carousel");
      const distance = () => track.scrollWidth - track.parentElement.clientWidth;
      gsap.to(track, {
        x: () => -distance(),
        ease: "none",
        scrollTrigger: {
          trigger: work,
          start: "top top",
          end: () => "+=" + distance(),
          pin: true,
          scrub: 1,
          anticipatePin: 1,
          invalidateOnRefresh: true,
          onUpdate: self => {
            gsap.set(bar, { scaleX: self.progress });
            count.textContent = `0${Math.min(cards, Math.round(self.progress * (cards - 1)) + 1)} / 0${cards}`;
          }
        }
      });
      return () => work.classList.remove("is-carousel");
    });

    // One-time gentle reveals: headings and card groups (opacity + y <= 20px, 0.6s, once)
    const reveal = (targets, trigger, stagger) => {
      if (!document.querySelector(trigger) || !document.querySelector(targets)) return;
      gsap.from(targets, {
        y: 20, opacity: 0, duration: 0.6, ease: "power2.out", stagger: stagger || 0,
        scrollTrigger: { trigger, start: "top 85%", once: true }
      });
    };
    gsap.utils.toArray(".sh").forEach(h => gsap.from(h.children, {
      y: 20, opacity: 0, duration: 0.6, ease: "power2.out", stagger: 0.06,
      scrollTrigger: { trigger: h, start: "top 85%", once: true }
    }));
    gsap.utils.toArray(".pcard").forEach(c => gsap.from(c, {
      y: 20, opacity: 0, duration: 0.6, ease: "power2.out",
      scrollTrigger: { trigger: c, start: "top 85%", once: true }
    }));
    reveal(".feature, .acards .acard", "#writing .wrap > :nth-child(2)", 0.08);
    reveal(".scard", ".stack", 0.08);
    reveal(".xrow", ".xp", 0.1);
    reveal(".bcard", ".beyond", 0.08);
    reveal(".contact", ".contact");
  };
  Promise.race([document.fonts.ready, new Promise(r => setTimeout(r, 1200))]).then(start);
})();
