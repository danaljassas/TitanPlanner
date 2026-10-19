const path = require("path");
const webpack = require("webpack");
const modules = [path.resolve(__dirname, "node_modules"), ...(process.env.NODE_PATH || "").split(path.delimiter).filter(Boolean)];
webpack({
    mode: "production",
    entry: path.resolve(__dirname, "viewer.js"),
    output: { path: __dirname, filename: "viewer.bundle.js" },
    resolve: { modules },
    performance: false,
    devtool: false
}, (error, stats) => {
    if (error || stats.hasErrors()) {
        console.error(error ? error.message : stats.toString({ all: false, errors: true }));
        process.exitCode = 1;
    } else {
        console.log(stats.toString({ all: false, assets: true }));
    }
});
