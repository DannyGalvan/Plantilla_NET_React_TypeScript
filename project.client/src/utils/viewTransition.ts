export const transitionViewIfSupported = (updateCb: () => void) => {
  if (document.startViewTransition) {
    document.startViewTransition(updateCb);
  } else {
    updateCb();
  }
};

export const retrase = async (ms: number) =>
  new Promise((res) => setTimeout(res, ms));

export const rebootScroll = () => {
  const scroll = document.querySelector("#scroll");
  scroll?.scrollTo({
    top: 0,
    behavior: "smooth",
  });

  const drawer = document.querySelector("#create");
  drawer?.scrollTo({
    top: 0,
    behavior: "smooth",
  });

  const drawerUpdate = document.querySelector("#update");
  drawerUpdate?.scrollTo({
    top: 0,
    behavior: "smooth",
  });
};
