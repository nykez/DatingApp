using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private readonly Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repository, IMapper mapper,
            IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _mapper = mapper;
            _repo = repository;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            // create new cloudinary instance from account info
            _cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name ="GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo = await _repo.GetPhoto(id);

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, 
            PhotoForCreationDto photoForCreationDto)
        {
            // invalid user
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }

            // get user
            var userFromRepo = await _repo.GetUser(userId);

            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();

            // we have a file
            if (file.Length > 0)
            {   
                // dispose properly
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        // only crop the face value
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };

                    // upload
                    uploadResult = _cloudinary.Upload(uploadParams);
                }

                photoForCreationDto.Url = uploadResult.Url.ToString();
                photoForCreationDto.PublicId = uploadResult.PublicId;

                // map our DTO into our photo itself based on the props

                var photo = _mapper.Map<Photo>(photoForCreationDto);

                // no main photo, make it the main photo then.
                if (!userFromRepo.Photos.Any(u => u.IsMain))
                    photo.IsMain = true;

                // save photo details
                userFromRepo.Photos.Add(photo);

                if (await _repo.SaveAll())
                {
                    var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                    // save is successful
                    return CreatedAtRoute("GetPhoto", new {userId = userId, id = photo.Id}, photoToReturn);
                }
            }

            return BadRequest("Could not add the photo.");
        }
    }
}