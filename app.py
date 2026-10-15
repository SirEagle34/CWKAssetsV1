from flask import Flask, render_template, abort

app = Flask(__name__)

PAGES = {
    "index": "index.html",
    "cwk_obsidian": "cwk_obsidian.html",
    "cwk_darkmagic": "cwk_darkmagic.html",
    "cwk_obsidian_creatures": "cwk_obsidian_creatures.html",
    "cwk_obsidian_actionscards": "cwk_obsidian_actionscards.html",
    "cwk_darkmagic_creatures": "cwk_darkmagic_creatures.html",
    "cwk_darkmagic_actionscards": "cwk_darkmagic_actionscards.html",
}

@app.get("/")
def home():
    return render_template(PAGES["index"])

@app.get("/<page>.html")
def html_page(page):
    template = PAGES.get(page)
    if not template:
        abort(404)
    return render_template(template)

@app.get("/health")
def health():
    return {"status": "ok", "app": "CWK Wiki"}

if __name__ == "__main__":
    app.run(debug=True)
