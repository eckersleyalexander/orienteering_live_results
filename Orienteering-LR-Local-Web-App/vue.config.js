module.exports = {
    outputDir:
        process.env.NODE_ENV === "production"
            ? "../Orienteering-LR-Desktop/bin/release/vue_app" // production build path
            : "../Orienteering-LR-Desktop/bin/Debug/vue_app" // debug build path
};
