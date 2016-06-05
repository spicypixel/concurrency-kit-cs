import * as gulp from "gulp";

import * as FileSystem from "@spicypixel-private/core-kit-js/lib/file-system";
import UnityEditor from "@spicypixel-private/unity-kit-js/lib/unity-editor";
import * as BuildKit from "@spicypixel-private/build-kit-js";

gulp.task("default", () => build());
gulp.task("clean", () => clean());
gulp.task("rebuild", () => rebuild());
gulp.task("build:code", () => buildCode());
gulp.task("build:docs", () => buildDocs());
gulp.task("build", () => build());
gulp.task("test", () => test());
gulp.task("install:monodocs", () => installMonoDocs());

let monoDocBuilder = new BuildKit.MonoDocBuilder();

async function clean() {
  await FileSystem.removePatternsAsync([
    "Source/**/bin/*", "Source/**/obj/*",
    "Doxygen/html", "Doxygen/xml", "Doxygen/qt",
    "MonoDoc/xml", "MonoDoc/assemble", "MonoDoc/html"]);
}

async function rebuild() {
  await clean();
  await build();
}

async function build() {
  await buildCode();
  await buildDocs();
}

async function buildCode() {
  await BuildKit.MSBuildBuilder.buildAsync({
    properties: {
      UnityEnginePath: UnityEditor.enginePath
    },
    toolsVersion: 12.0,
    targets: ["Build"],
    errorOnFail: true,
    stdout: true
  });
}

async function installMonoDocs() {
  await monoDocBuilder.installAsync();
}

async function buildDocs() {
  await BuildKit.DoxygenBuilder.buildAsync();

  let assemblies = [
    "System.Threading",
    "SpicyPixel.Threading",
    "SpicyPixel.Threading.Unity"];
  await monoDocBuilder.buildAsync("SpicyPixel.ConcurrencyKit", assemblies);
}

async function test() {
  await buildCode();
  await BuildKit.NUnitRunner.runAsync();
}