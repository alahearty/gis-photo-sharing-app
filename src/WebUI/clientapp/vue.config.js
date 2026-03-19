const webpack = require("webpack");

module.exports = {
  publicPath: process.env.NODE_ENV === "production" ? "/" : "/",
  outputDir: "dist",
  configureWebpack: {
    plugins: [
      new webpack.ProvidePlugin({
        mapboxgl: "mapbox-gl",
      }),
    ],
  },
  devServer: {
    port: 8080,
    proxy: {
      "/api": {
        target: "https://localhost:5001",
        secure: false,
      },
      "/uploads": {
        target: "https://localhost:5001",
        secure: false,
      },
    },
  },
};
