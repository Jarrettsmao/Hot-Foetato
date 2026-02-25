function Play() {
  return (
    <div style={{ width: "100vw", height: "100vh", overflow: "hidden" }}>
      <iframe
        src="/unity/index.html"
        title="Hot Foetato"
        style={{
          width: "100%",
          height: "100%",
          border: "none",
        }}
        allow="autoplay; fullscreen"
        allowFullScreen
      />
    </div>
  );
}

export default Play;
