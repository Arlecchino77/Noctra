// NØCTRA — клиентская логика. Все данные страница получает и сохраняет через API (/api/...).
(function () {
    "use strict";

    const $ = (sel, root = document) => root.querySelector(sel);
    const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));
    const money = v => `${Math.round(v)}$`;
    const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));

    // ---------- Уведомления ----------
    let toastTimer;
    function toast(text) {
        const el = $("[data-toast]");
        if (!el) return;
        el.textContent = text;
        el.classList.add("is-shown");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => el.classList.remove("is-shown"), 2200);
    }

    async function api(url, options = {}) {
        const res = await fetch(url, {
            headers: { "Content-Type": "application/json", "Accept": "application/json" },
            credentials: "same-origin",
            ...options,
        });
        if (res.status === 401) {
            location.href = "/Account/Login?returnUrl=" + encodeURIComponent(location.pathname + location.search);
            throw new Error("unauthorized");
        }
        const data = res.status === 204 ? null : await res.json().catch(() => null);
        if (!res.ok) {
            const msg = data?.error || (data?.errors && Object.values(data.errors).flat()[0]) || "Something went wrong";
            throw new Error(msg);
        }
        return data;
    }

    function setCartCount(count) {
        $$("[data-cart-count]").forEach(el => el.textContent = String(count).padStart(2, "0"));
    }

    // ---------- Добавление в корзину (страница товара) ----------
    const addForm = $("[data-add-to-cart]");
    if (addForm) {
        addForm.addEventListener("submit", async e => {
            e.preventDefault();
            const size = $('input[name="size"]:checked')?.value;
            if (!size) { toast("CHOOSE A SIZE"); $(".sizes")?.focus(); return; }
            const btn = $("button[type=submit]", addForm);
            btn.disabled = true;
            try {
                const cart = await api("/api/cart", {
                    method: "POST",
                    body: JSON.stringify({ productId: Number(addForm.dataset.addToCart), size, quantity: 1 }),
                });
                setCartCount(cart.count);
                toast("ADDED TO CART");
            } catch (err) {
                if (err.message !== "unauthorized") toast(err.message.toUpperCase());
            } finally {
                btn.disabled = false;
            }
        });
    }

    // ---------- Корзина ----------
    const cartList = $("[data-cartlist]");
    if (cartList) {
        const preview = {
            name: $("[data-preview-name]"),
            img: $("[data-preview-img]"),
            link: $("[data-preview-link]"),
            size: $("[data-preview-size]"),
            price: $("[data-preview-price]"),
        };

        function showPreview(item) {
            $$(".citem", cartList).forEach(i => i.classList.toggle("is-active", i === item));
            if (preview.name) preview.name.textContent = item.dataset.name;
            if (preview.img && item.dataset.img) preview.img.src = item.dataset.img;
            if (preview.link) preview.link.href = "/Shop/Product/" + item.dataset.productId;
            if (preview.size) preview.size.textContent = "SIZE " + item.dataset.size;
            if (preview.price) preview.price.textContent = item.dataset.price + "$";
        }

        function render(cart) {
            setCartCount(cart.count);
            $("[data-cart-total]").textContent = money(cart.total);
            $("[data-order]").disabled = cart.items.length === 0;
            const ids = new Set(cart.items.map(i => String(i.id)));
            $$(".citem", cartList).forEach(el => {
                if (!ids.has(el.dataset.item)) { el.remove(); return; }
                const it = cart.items.find(i => String(i.id) === el.dataset.item);
                $("[data-qty-value]", el).textContent = it.quantity;
                $("[data-item-total]", el).textContent = money(it.total);
            });
            if (cart.items.length === 0) location.reload();
        }

        const first = $(".citem", cartList);
        if (first) first.classList.add("is-active");

        cartList.addEventListener("mouseover", e => {
            const item = e.target.closest(".citem");
            if (item) showPreview(item);
        });
        cartList.addEventListener("focusin", e => {
            const item = e.target.closest(".citem");
            if (item) showPreview(item);
        });

        cartList.addEventListener("click", async e => {
            const item = e.target.closest(".citem");
            if (!item) return;
            const id = item.dataset.item;
            try {
                if (e.target.closest("[data-remove]")) {
                    render(await api(`/api/cart/${id}`, { method: "DELETE" }));
                    toast("REMOVED");
                } else if (e.target.closest("[data-qty]")) {
                    const delta = Number(e.target.closest("[data-qty]").dataset.qty);
                    const current = Number($("[data-qty-value]", item).textContent);
                    const quantity = current + delta;
                    if (quantity < 1) render(await api(`/api/cart/${id}`, { method: "DELETE" }));
                    else if (quantity <= 20) render(await api(`/api/cart/${id}`, { method: "PUT", body: JSON.stringify({ quantity }) }));
                }
            } catch (err) {
                if (err.message !== "unauthorized") toast(err.message.toUpperCase());
            }
        });

        $("[data-order]")?.addEventListener("click", async e => {
            e.currentTarget.disabled = true;
            try {
                const res = await api("/api/cart/order", { method: "POST" });
                toast(`ORDER PLACED · ${money(res.total)}`);
                setTimeout(() => location.reload(), 1400);
            } catch (err) {
                toast(err.message.toUpperCase());
                e.currentTarget.disabled = false;
            }
        });
    }

    // ---------- Каталог: фильтры без перезагрузки страницы ----------
    const filters = $("[data-filters]");
    const catalog = $("[data-catalog]");
    if (filters && catalog) {
        let seq = 0;

        function tile(p) {
            const img = p.imageUrls[0] ? `<img src="${esc(p.imageUrls[0])}" alt="${esc(p.name)}" loading="lazy" />` : "";
            return `<a class="tile" href="/Shop/Product/${p.id}">${img}` +
                `<span class="tile__label">${esc(p.name)}</span>` +
                `<span class="tile__price">${money(p.price)}</span></a>`;
        }

        async function apply() {
            const params = new URLSearchParams();
            for (const [k, v] of new FormData(filters)) if (v !== "") params.append(k, v);
            const my = ++seq;
            history.replaceState(null, "", "/Shop/Catalog" + (params.toString() ? "?" + params : ""));
            try {
                const products = await api("/api/products?" + params);
                if (my !== seq) return; // пришёл ответ на устаревший запрос
                catalog.innerHTML = products.length
                    ? products.map(tile).join("")
                    : `<div class="empty" style="grid-column: 1 / -1">NOTHING FOUND</div>`;
                $("[data-count]").textContent = products.length;
            } catch (err) {
                toast(err.message.toUpperCase());
            }
        }

        let debounce;
        filters.addEventListener("change", apply);
        filters.addEventListener("input", e => {
            if (e.target.type !== "number") return;
            clearTimeout(debounce);
            debounce = setTimeout(apply, 350);
        });
        filters.addEventListener("submit", e => { e.preventDefault(); apply(); });
    }

    // ---------- Таймер до следующего показа (лента новостей) ----------
    const timer = $("[data-timer]");
    if (timer) {
        const tick = () => {
            const now = new Date();
            const next = new Date(now);
            next.setHours(24, 0, 0, 0);
            const s = Math.floor((next - now) / 1000);
            const pad = n => String(n).padStart(2, "0");
            timer.textContent = `${pad(Math.floor(s / 3600))}:${pad(Math.floor(s / 60) % 60)}:${pad(s % 60)}`;
        };
        tick();
        setInterval(tick, 1000);
    }
})();
