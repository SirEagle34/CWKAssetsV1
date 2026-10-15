(() => {
  "use strict";

  const search = document.querySelector("[data-search-input]");
  const count = document.querySelector("[data-result-count]");
  const empty = document.querySelector("[data-empty-state]");
  const catalogue = document.querySelector("[data-catalogue]");
  if (!search || !count || !empty || !catalogue) return;

  const page = document.body.dataset;
  const edition = page.edition;
  const kind = page.kind;
  if (!edition || !kind) return;

  const firstValue = (item, keys) => {
    for (const key of keys) {
      const value = item && item[key];
      if (value !== undefined && value !== null && String(value).trim() !== "") return value;
    }
    return "";
  };

  const textValue = value => {
    if (typeof value === "string" || typeof value === "number") return String(value);
    return "";
  };

  const titleKeys = ["Name", "name", "DisplayName", "Title", "title", "ID", "Id", "id"];
  const idKeys = ["ID", "Id", "id", "Name", "name"];
  const imageKeys = kind === "creatures"
    ? ["UITexture", "PortraitTexture", "SpriteName", "Icon", "IconPath", "IconName", "Portrait", "Image", "Texture", "CardIcon", "ID"]
    : ["UITexture", "PortraitTexture", "SpriteName", "Icon", "IconPath", "IconName", "Portrait", "Image", "Texture", "CardPortrait", "ActionPortrait", "ID"];
  const descriptionKeys = ["Description", "description", "Effect", "EffectText", "Text", "RulesText", "FlavorText"];
  const metaKeys = ["Rarity", "Faction", "Cost", "EnergyCost", "Type", "CardType", "Attack", "Health", "Level", "HP", "STR", "INT", "DEX", "DEF", "RES"];
  const factionNames = {
    Blue: "Blue Plains",
    Red: "Cornfields",
    Light: "Nicelands",
    Green: "Sandlands",
    Dark: "Useless Swamp",
    White: "Icylands",
    Purple: "Spookyland",
    Orange: "Magmalands",
    Rainbow: "Rainbow/Special",
    Colorless: "Rainbow/Special"
  };
  const factionIconNames = {
    Colorless: "Rainbow",
    Rainbow: "Rainbow"
  };

  let allItems = [];

  function assetUrl(value) {
    if (!value) return "";
    const raw = String(value).replace(/\\/g, "/").replace(/^\/+/, "");
    return "/wiki-asset/" + encodeURIComponent(edition) + "/" +
      raw.split("/").map(encodeURIComponent).join("/");
  }

  function render() {
    const query = search.value.trim().toLocaleLowerCase();
    const filtered = allItems.filter(item => {
      const haystack = JSON.stringify(item).toLocaleLowerCase();
      return !query || haystack.includes(query);
    });

    catalogue.replaceChildren();
    empty.hidden = filtered.length > 0;
    if (allItems.length === 0) {
      count.textContent = "0 entries";
      return;
    }
    count.textContent = filtered.length + " / " + allItems.length + " entries";

    for (const item of filtered) {
      const card = document.createElement("article");
      card.className = "wiki-entry";
      const title = textValue(firstValue(item, titleKeys)) || "Unnamed entry";
      const id = textValue(firstValue(item, idKeys));
      const number = textValue(item && item.Number).trim();
      const imageCandidates = [...new Set(imageKeys
        .map(key => textValue(item && item[key]).trim())
        .filter(Boolean))];
      const description = textValue(firstValue(item, descriptionKeys));

      if (number) {
        const numberBadge = document.createElement("span");
        numberBadge.className = "wiki-entry-number";
        numberBadge.textContent = "#" + number.replace(/^#/, "");
        numberBadge.setAttribute("aria-label", "Number " + number.replace(/^#/, ""));
        card.appendChild(numberBadge);
      }

      if (imageCandidates.length) {
        const img = document.createElement("img");
        img.className = "wiki-entry-image";
        img.loading = "lazy";
        img.alt = title;
        let imageIndex = 0;
        const tryNextImage = () => {
          imageIndex += 1;
          if (imageIndex >= imageCandidates.length) {
            img.hidden = true;
            return;
          }
          img.src = assetUrl(imageCandidates[imageIndex]);
        };
        img.addEventListener("error", tryNextImage);
        img.src = assetUrl(imageCandidates[imageIndex]);
        card.appendChild(img);
      }

      const body = document.createElement("div");
      body.className = "wiki-entry-body";
      const heading = document.createElement("h2");
      heading.textContent = title;
      body.appendChild(heading);

      if (id && id !== title) {
        const idText = document.createElement("p");
        idText.className = "wiki-entry-id";
        idText.textContent = id;
        body.appendChild(idText);
      }

      if (description) {
        const desc = document.createElement("p");
        desc.className = "wiki-entry-description";
        desc.textContent = description;
        body.appendChild(desc);
      }

      if (kind === "creatures") {
        const passiveDescription = textValue(item && item.Passives).trim();
        if (passiveDescription) {
          const passive = document.createElement("p");
          passive.className = "wiki-entry-passives";
          const passiveLabel = document.createElement("strong");
          passiveLabel.textContent = "Passives: ";
          passive.appendChild(passiveLabel);
          passive.appendChild(document.createTextNode(passiveDescription));
          body.appendChild(passive);
        }
      }

      const metadata = document.createElement("div");
      metadata.className = "wiki-entry-meta";
      for (const key of metaKeys) {
        const rawValue = textValue(item && item[key]);
        if (!rawValue) continue;
        const value = key === "Faction"
          ? (factionNames[rawValue.trim()] || rawValue)
          : rawValue;
        const chip = document.createElement("span");
        if (key === "Faction") {
          const faction = rawValue.trim();
          const iconFaction = factionIconNames[faction] || faction;
          chip.classList.add("wiki-faction", "wiki-faction-" + faction.toLowerCase());
          const factionIcon = document.createElement("img");
          factionIcon.className = "wiki-faction-icon";
          factionIcon.alt = "";
          factionIcon.width = 16;
          factionIcon.height = 16;
          factionIcon.src = "/faction-icon/" + encodeURIComponent(edition) + "/" + encodeURIComponent(iconFaction) + ".png";
          factionIcon.addEventListener("error", () => factionIcon.remove());
          chip.appendChild(factionIcon);
        }
        chip.appendChild(document.createTextNode(key + ": " + value));
        metadata.appendChild(chip);
      }
      if (metadata.childElementCount) body.appendChild(metadata);
      card.appendChild(body);
      catalogue.appendChild(card);
    }
  }

  search.addEventListener("input", render);

  fetch("/api/catalogue/" + encodeURIComponent(edition) + "/" + encodeURIComponent(kind))
    .then(response => {
      if (!response.ok) throw new Error(response.status === 503
        ? "The edition ZIP archive is not installed or its data files are missing."
        : "Could not load the catalogue.");
      return response.json();
    })
    .then(data => {
      allItems = Array.isArray(data.items) ? data.items : [];
      empty.querySelector("h2").textContent = allItems.length
        ? "No matching entries"
        : "No catalogue entries found";
      empty.querySelector("p").textContent = allItems.length
        ? "Try another search term."
        : "The JSON file was read, but it did not contain a list of entries.";
      render();
    })
    .catch(error => {
      count.textContent = "Data unavailable";
      empty.hidden = false;
      empty.querySelector("h2").textContent = "Catalogue could not load";
      empty.querySelector("p").textContent = error.message;
      console.error(error);
    });
})();