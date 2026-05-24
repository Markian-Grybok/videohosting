import React, { useRef, useEffect, useState } from "react";
import Hls from "hls.js";
import Spinner from "../ui/Spinner";
import { fileApi } from "../../api/fileApi";
import type { QualityOption } from "../../types";

interface VideoPlayerProps {
  fileId: string;
  initialUrl: string; // master.m3u8
  availableQualities: string[]; // ["360p","480p","720p","1080p"]
}

const VideoPlayer: React.FC<VideoPlayerProps> = ({ fileId, initialUrl, availableQualities }) => {
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const [hlsInstance, setHlsInstance] = useState<Hls | null>(null);
  const [selectedQuality, setSelectedQuality] = useState<string>("auto");
  const [isChangingQuality, setIsChangingQuality] = useState<boolean>(false);

  const qualityOptions: QualityOption[] = [
    { label: "Auto", value: "auto" },
    ...availableQualities
      .slice()
      .reverse()
      .map(q => ({ label: q, value: q }))
  ];

  // Initialize HLS with the master (initialUrl)
  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;

    let hls: Hls | null = null;

    if (Hls.isSupported()) {
      hls = new Hls({
        startLevel: -1,
      });
      hls.loadSource(initialUrl);
      hls.attachMedia(video);
      setHlsInstance(hls);
    } else if (video.canPlayType("application/vnd.apple.mpegurl")) {
      video.src = initialUrl;
    }

    return () => {
      if (hls) {
        hls.destroy();
      }
      setHlsInstance(null);
    };
  }, [initialUrl]);

  const handleQualityChange = async (quality: string) => {
    setSelectedQuality(quality);

    const video = videoRef.current;
    if (!video) return;

    // Switch to auto (master.m3u8 + ABR)
    if (quality === "auto") {
      if (hlsInstance) {
        try {
          hlsInstance.currentLevel = -1; // -1 = auto
        } catch (err) {
          console.error("Failed to switch to auto quality", err);
        }
      } else {
        // native HLS (Safari) — reload initialUrl
        video.src = initialUrl;
      }
      return;
    }

    // Specific quality — fetch presigned URL and reload HLS
    setIsChangingQuality(true);
    try {
      const result = await fileApi.getQualityUrl(fileId, quality);
      const currentTime = video?.currentTime ?? 0;

      if (Hls.isSupported() && hlsInstance) {
        hlsInstance.loadSource(result.url);
        // Wait for manifest parsed to resume playback and position
        hlsInstance.once(Hls.Events.MANIFEST_PARSED, () => {
          if (video) {
            video.currentTime = currentTime;
            video.play().catch(() => {});
          }
        });
      } else if (video) {
        // Safari native fallback
        video.src = result.url;
        video.currentTime = currentTime;
        video.play().catch(() => {});
      }
    } catch (err) {
      console.error("Failed to load quality:", err);
    } finally {
      setIsChangingQuality(false);
    }
  };

  return (
    <div className="w-full">
      <video
        ref={videoRef}
        controls
        className="w-full rounded-xl bg-black aspect-video"
        playsInline
      />

      <div className="flex items-center justify-end gap-2 mt-2">
        {isChangingQuality && <Spinner size="sm" />}
        <span className="text-xs text-gray-500">Якість:</span>
        <div className="flex gap-1">
          {qualityOptions.map(opt => (
            <button
              key={opt.value}
              onClick={() => handleQualityChange(opt.value)}
              className={`px-2 py-1 text-xs rounded font-medium transition-colors ${
                selectedQuality === opt.value
                  ? "bg-primary-600 text-white"
                  : "bg-gray-100 text-gray-600 hover:bg-gray-200"
              }`}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
};

export default VideoPlayer;