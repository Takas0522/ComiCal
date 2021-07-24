import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'imageSrc'
})
export class ImageSrcPipe implements PipeTransform {

  transform(value: string): unknown {
    return `data:image;base64,${value}`;
  }

}
