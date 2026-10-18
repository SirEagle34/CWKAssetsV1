(() => {
  "use strict";

  const search = document.querySelector("[data-search-input]");
  const factionFilter = document.querySelector("[data-faction-filter]");
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
    Black: "Obsidian",
    White: "Icylands",
    Purple: "Spookyland",
    Orange: "Magmalands",
    Yellow: "Buildings",
    Rainbow: "Rainbow/Special",
    Colorless: "Rainbow/Special"
  };
  const factionIconNames = {
    Colorless: "Rainbow",
    Rainbow: "Rainbow"
  };

  let allItems = [];
  let actionCardsById = new Map();

  function assetUrl(value) {
    if (!value) return "";
    const raw = String(value).replace(/\\/g, "/").replace(/^\/+/, "");
    return "/wiki-asset/" + encodeURIComponent(edition) + "/" +
      raw.split("/").map(encodeURIComponent).join("/");
  }

  function render() {
    const query = search.value.trim().toLocaleLowerCase();
    const selectedFaction = factionFilter ? factionFilter.value : "";
    const filtered = allItems.filter(item => {
      const haystack = JSON.stringify(item).toLocaleLowerCase();
      const itemFaction = textValue(firstValue(item, ["Faction", "faction"])).trim();
      const matchesSearch = !query || haystack.includes(query);
      const matchesFaction = !selectedFaction || itemFaction.toLocaleLowerCase() === selectedFaction.toLocaleLowerCase();
      return matchesSearch && matchesFaction;
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

      if (kind === "creatures") {
        const passiveData = firstValue(item, ["CreaturePassives", "Passives"]);
        let passiveDescription = "";
        if (typeof passiveData === "string" || typeof passiveData === "number") {
          passiveDescription = textValue(passiveData).trim();
        } else if (Array.isArray(passiveData)) {
          passiveDescription = passiveData.map(passive =>
            typeof passive === "object" && passive !== null
              ? textValue(firstValue(passive, ["Description", "description", "Effect", "EffectText", "Text"]))
              : textValue(passive)
          ).filter(Boolean).join(" ");
        } else if (passiveData && typeof passiveData === "object") {
          passiveDescription = textValue(firstValue(passiveData, ["Description", "description", "Effect", "EffectText", "Text"])).trim();
        }

        if (passiveDescription) {
          const passive = document.createElement("p");
          passive.className = "wiki-entry-passives";
          const passiveLabel = document.createElement("strong");
          passiveLabel.textContent = "Creature Passive: ";
          passive.appendChild(passiveLabel);
          passive.appendChild(document.createTextNode(passiveDescription));
          body.appendChild(passive);
        }

        const spellCards = [];
        for (let slot = 1; slot <= 5; slot += 1) {
          const cardId = textValue(firstValue(item, ["ActionCard" + slot, "SpellCard" + slot])).trim();
          if (!cardId || cardId.toLowerCase() === "none" || cardId === "0") continue;
          const actionCard = actionCardsById.get(cardId);
          const cardName = actionCard ? textValue(firstValue(actionCard, titleKeys)) : cardId;
          const cardDescription = actionCard
            ? textValue(firstValue(actionCard, descriptionKeys)).trim()
            : "";
          spellCards.push({ slot, cardId, cardName, cardDescription });
        }

        if (spellCards.length) {
          const spellSection = document.createElement("div");
          spellSection.className = "wiki-entry-spell-cards";
          const spellHeading = document.createElement("strong");
          spellHeading.className = "wiki-entry-spell-heading";
          spellHeading.textContent = "Spell Cards";
          spellSection.appendChild(spellHeading);
          const ordinals = ["1st", "2nd", "3rd", "4th", "5th"];
          spellCards.forEach(spell => {
            const row = document.createElement("p");
            row.className = "wiki-entry-spell-card";
            row.textContent = ordinals[spell.slot - 1] + " Spell Card: " + spell.cardName + " / " + spell.cardId;
            spellSection.appendChild(row);
            if (spell.cardDescription) {
              const description = document.createElement("p");
              description.className = "wiki-entry-spell-description";
              description.textContent = "Description: " + spell.cardDescription;
              spellSection.appendChild(description);
            }
          });
          body.appendChild(spellSection);
        }
      }
      card.appendChild(body);
      catalogue.appendChild(card);
    }
  }

  search.addEventListener("input", render);
  if (factionFilter) factionFilter.addEventListener("change", render);

  const catalogueRequests = [
    fetch("/api/catalogue/" + encodeURIComponent(edition) + "/" + encodeURIComponent(kind)),
  ];
  if (kind === "creatures") catalogueRequests.push(fetch("/api/catalogue/" + encodeURIComponent(edition) + "/actioncards"));

  Promise.all(catalogueRequests)
    .then(async responses => {
      for (const response of responses) {
        if (!response.ok) throw new Error(response.status === 503
          ? "The edition ZIP archive is not installed or its data files are missing."
          : "Could not load the catalogue.");
      }
      const data = await responses[0].json();
      if (kind === "creatures" && responses[1]) {
        const actionCardData = await responses[1].json();
        actionCardsById = new Map((Array.isArray(actionCardData.items) ? actionCardData.items : [])
          .filter(card => card && card.ID)
          .map(card => [String(card.ID).trim(), card]));
      }
      return data;
    })
    .then(data => {
      allItems = Array.isArray(data.items) ? data.items : [];
      if (factionFilter) {
        const factions = [...new Set(allItems
          .map(item => textValue(firstValue(item, ["Faction", "faction"])).trim())
          .filter(Boolean))]
          .sort((a, b) => a.localeCompare(b, undefined, { sensitivity: "base" }));
        factionFilter.replaceChildren();
        const allOption = document.createElement("option");
        allOption.value = "";
        allOption.textContent = "All Factions";
        factionFilter.appendChild(allOption);
        factions.forEach(faction => {
          const option = document.createElement("option");
          option.value = faction;
          option.textContent = factionNames[faction] || faction;
          factionFilter.appendChild(option);
        });
        factionFilter.disabled = factions.length === 0;
      }
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