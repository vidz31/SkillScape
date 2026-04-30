import * as React from "react";
import * as ScrollAreaPrimitive from "@radix-ui/react-scroll-area";

const ScrollArea = ScrollAreaPrimitive.Root;
const ScrollBar = ({ className, ...props }) => (
  <ScrollAreaPrimitive.Scrollbar className={className} {...props} />
);
const ScrollAreaViewport = ScrollAreaPrimitive.Viewport;
const ScrollAreaCorner = ScrollAreaPrimitive.Corner;

export { ScrollArea, ScrollBar, ScrollAreaViewport, ScrollAreaCorner };
